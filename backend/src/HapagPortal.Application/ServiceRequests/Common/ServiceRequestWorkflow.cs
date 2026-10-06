namespace HapagPortal.Application.ServiceRequests.Common;

using System.Net.Mail;
using System.Text.Json;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using Microsoft.EntityFrameworkCore;

/// <summary>Quién ejecuta un paso de la solicitud: cliente, equipo interno o sistema (liberación del pago).</summary>
public sealed record ServiceActor(string Name, Guid? UserId, string Kind)
{
    public static readonly ServiceActor System = new("SYSTEM", null, ServiceRequestActorKinds.System);

    public static ServiceActor Client(ICurrentUserService currentUserService) =>
        new(currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "client", currentUserService.UserId, ServiceRequestActorKinds.Client);

    public static ServiceActor Internal(ICurrentUserService currentUserService) =>
        new(currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "internal", currentUserService.UserId, ServiceRequestActorKinds.Internal);
}

/// <summary>Datos de facturación ingresados por el cliente.</summary>
public sealed record ServiceBillingInput(string? TaxId, string? Name, string? Address, string? Email, string? Activity);

/// <summary>
/// Flujo estándar de las solicitudes on demand (M2-03, M2-04): validación de datos y facturación, cálculo de la
/// tarifa vigente al enviar (NF-22), derivación al equipo que aprueba (Drop Off al equipo ED, M3-09), cobro
/// como cargo local del BL que se paga por el carro (M5-01) y prestación posterior al pago. Cada transición
/// queda en la línea de tiempo con fecha y actor. No guarda: lo hace el llamador.
/// </summary>
public sealed class ServiceRequestWorkflow(
    IApplicationDbContext dbContext,
    ServiceCatalogEvaluator catalog,
    INotificationPublisher notificationPublisher)
{
    public const int MaxBillingNameLength = 200;

    /// <summary>Transición validada por <see cref="ServiceRequestStateMachine"/>, con su hito en la línea de tiempo.</summary>
    public static Result Transition(
        IApplicationDbContext dbContext,
        ServiceRequest request,
        string to,
        ServiceActor actor,
        string? notes,
        DateTime now)
    {
        if (!ServiceRequestStateMachine.CanTransition(request.Status, to))
            return Result.Failure(DomainErrors.ServiceRequest.InvalidTransition(request.Status, to));

        AddEvent(dbContext, request, request.Status, to, actor, notes, now);
        request.Status = to;
        request.StatusChangedAt = now;

        switch (to)
        {
            case ServiceRequestStatus.Submitted: request.SubmittedAt = now; break;
            case ServiceRequestStatus.Approved: request.ApprovedAt = now; break;
            case ServiceRequestStatus.Rejected: request.RejectedAt = now; break;
            case ServiceRequestStatus.Paid: request.PaidAt ??= now; break;
            case ServiceRequestStatus.Completed: request.CompletedAt = now; break;
            case ServiceRequestStatus.Cancelled: request.CancelledAt = now; break;
        }

        return Result.Success();
    }

    public static ServiceRequestEvent AddEvent(
        IApplicationDbContext dbContext,
        ServiceRequest request,
        string? from,
        string to,
        ServiceActor actor,
        string? notes,
        DateTime now)
    {
        var entry = new ServiceRequestEvent
        {
            ServiceRequestId = request.Id,
            Sequence = ++request.TimelineSequence,
            FromStatus = from,
            ToStatus = to,
            OccurredAt = now,
            ActorUserId = actor.UserId,
            ActorName = actor.Name,
            ActorKind = actor.Kind,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        dbContext.ServiceRequestEvents.Add(entry);
        return entry;
    }

    /// <summary>Valida los datos contra el formulario; los campos de archivo se cumplen con los adjuntos.</summary>
    public async Task<Result<ServiceInputValidation>> ValidateInputAsync(
        ServiceDefinition definition,
        BillOfLading billOfLading,
        Guid? requestId,
        string? valuesJson,
        bool requireComplete,
        CancellationToken cancellationToken)
    {
        var fields = ServiceInputSchema.Parse(definition.InputSchemaJson)
            ?? throw new InvalidOperationException($"The form of the service '{definition.Code}' is not valid JSON.");

        var containers = await dbContext.BLContainers.AsNoTracking()
            .Where(c => c.BillOfLadingId == billOfLading.Id)
            .Select(c => c.ContainerNumber)
            .ToListAsync(cancellationToken);

        var attached = requestId is null
            ? []
            : await dbContext.ServiceRequestAttachments.AsNoTracking()
                .Where(a => a.ServiceRequestId == requestId.Value)
                .Select(a => a.FieldKey)
                .Distinct()
                .ToListAsync(cancellationToken);

        var validation = ServiceInputSchema.ValidateValues(fields, valuesJson, containers, attached, requireComplete);
        return validation.IsValid
            ? Result<ServiceInputValidation>.Success(validation)
            : ValidationResult<ServiceInputValidation>.WithErrors(
                validation.Errors.Select(e => new Error($"InputValues.{e.Field}", e.Message)).ToArray());
    }

    /// <summary>
    /// Datos de facturación (M3-07 a M3-14): obligatorios si la definición lo exige; el RUT debe ser el de la
    /// organización o el de un mandante cuyo acceso vigente sobre el BL habilita el servicio (M5-09).
    /// </summary>
    public async Task<Result> ValidateBillingAsync(
        ServiceDefinition definition,
        ServiceBillingInput billing,
        Client organization,
        ShipmentPermissionSet permissions,
        bool requireComplete,
        CancellationToken cancellationToken)
    {
        var complete = !string.IsNullOrWhiteSpace(billing.TaxId)
            && !string.IsNullOrWhiteSpace(billing.Name)
            && !string.IsNullOrWhiteSpace(billing.Address)
            && IsEmail(billing.Email);

        if (requireComplete && definition.BillingDataRequired && !complete)
            return Result.Failure(DomainErrors.ServiceRequest.BillingDataRequired);

        if (!string.IsNullOrWhiteSpace(billing.Email) && !IsEmail(billing.Email))
            return Result.Failure(DomainErrors.ServiceRequest.BillingDataRequired);

        if (string.IsNullOrWhiteSpace(billing.TaxId))
            return Result.Success();

        var taxId = TaxIdNormalizer.Normalize(billing.TaxId);
        if (TaxIdNormalizer.AreEqual(taxId, organization.TaxId))
            return Result.Success();

        foreach (var grant in permissions.Grants.Where(g => g.Actions.Contains(definition.ActionCode)))
        {
            var grantor = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == grant.GrantorOrganizationId, cancellationToken);
            if (grantor is not null && TaxIdNormalizer.AreEqual(taxId, grantor.TaxId))
                return Result.Success();
        }

        return Result.Failure(DomainErrors.ServiceRequest.BillingTaxIdNotAllowed);
    }

    public static void WriteBilling(ServiceRequest request, ServiceBillingInput billing)
    {
        request.BillingTaxId = string.IsNullOrWhiteSpace(billing.TaxId) ? null : TaxIdNormalizer.Normalize(billing.TaxId);
        request.BillingName = Trim(billing.Name, MaxBillingNameLength);
        request.BillingAddress = Trim(billing.Address, 300);
        request.BillingEmail = Trim(billing.Email, 256);
        request.BillingActivity = Trim(billing.Activity, 200);
    }

    /// <summary>
    /// Envía un borrador: condiciones vigentes, datos completos, facturación, tarifa del tramo vigente en este
    /// instante (aceptada por el cliente si la definición lo exige) y derivación al equipo que aprueba o al cobro.
    /// </summary>
    public async Task<Result> SubmitAsync(
        ServiceRequest request,
        ServiceDefinition definition,
        ServiceShipmentContext context,
        ServiceActor actor,
        bool acceptTariff,
        decimal? acceptedTotal,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Status != ServiceRequestStatus.Draft)
            return Result.Failure(DomainErrors.ServiceRequest.InvalidTransition(request.Status, ServiceRequestStatus.Submitted));

        var containerCount = await dbContext.BLContainers.AsNoTracking().CountAsync(c => c.BillOfLadingId == context.BillOfLading.Id, cancellationToken);
        var availability = await catalog.EvaluateAsync(definition, context.BillOfLading, containerCount, now, request.Id, cancellationToken);
        if (!availability.Available)
            return Result.Failure(DomainErrors.ServiceRequest.NotAvailable(string.Join(", ", availability.Reasons)));

        var input = await ValidateInputAsync(definition, context.BillOfLading, request.Id, request.InputValuesJson, requireComplete: true, cancellationToken);
        if (input.IsFailure)
            return input;

        var billing = await ValidateBillingAsync(
            definition,
            new ServiceBillingInput(request.BillingTaxId, request.BillingName, request.BillingAddress, request.BillingEmail, request.BillingActivity),
            context.Organization,
            context.Permissions,
            requireComplete: true,
            cancellationToken);
        if (billing.IsFailure)
            return billing;

        var computed = await catalog.QuoteAsync(
            new ServiceQuoteInput(definition, context.BillOfLading, context.Organization, context.Permissions,
                input.Value.Containers, input.Value.Numbers, now, request.Id),
            cancellationToken);
        if (computed.IsFailure)
            return computed;

        var quote = computed.Value.Quote;
        if (definition.TariffAcceptanceRequired && quote.RequiresPayment)
        {
            if (!acceptTariff)
                return Result.Failure(DomainErrors.ServiceRequest.TariffNotAccepted);
            if (acceptedTotal is not null && acceptedTotal.Value != quote.TotalAmount)
                return Result.Failure(DomainErrors.ServiceRequest.TariffChanged(quote.TotalAmount, quote.Currency ?? string.Empty));
            request.TariffAcceptedAt = now;
        }

        request.InputValuesJson = input.Value.NormalizedJson;
        request.ContainerNumbers = input.Value.Containers.Count == 0 ? null : string.Join(',', input.Value.Containers);
        ApplyQuote(request, quote);

        // Cargos del sistema de origen: se vinculan a la solicitud y se registran sus exenciones (M4-02).
        foreach (var charge in computed.Value.SourceCharges)
            dbContext.ServiceRequestCharges.Add(new ServiceRequestCharge { ServiceRequestId = request.Id, LocalChargeId = charge.Id });
        ApplyExemptions(computed.Value, context, actor, now);

        var submitted = Transition(dbContext, request, ServiceRequestStatus.Submitted, actor, null, now);
        if (submitted.IsFailure)
            return submitted;

        if (definition.ApprovalTeam != ServiceTeams.None)
        {
            request.AssignedTeam = definition.ApprovalTeam;
            return Transition(dbContext, request, ServiceRequestStatus.PendingApproval, ServiceActor.System,
                $"Derivada al equipo {definition.ApprovalTeam}.", now);
        }

        return await SettleAsync(request, definition, ServiceActor.System, now, cancellationToken);
    }

    /// <summary>Aprobación del equipo interno: continúa al cobro con la tarifa calculada al enviar.</summary>
    public async Task<Result> ApproveAsync(
        ServiceRequest request,
        ServiceDefinition definition,
        ServiceActor actor,
        string? notes,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var approved = Transition(dbContext, request, ServiceRequestStatus.Approved, actor, notes, now);
        if (approved.IsFailure)
            return approved;

        request.AssignedTeam = null;
        if (!string.IsNullOrWhiteSpace(notes))
            request.ResolutionNotes = notes.Trim();

        return await SettleAsync(request, definition, ServiceActor.System, now, cancellationToken);
    }

    /// <summary>
    /// Tras el envío (sin aprobación) o la aprobación: con monto a pagar genera el cargo local con la tarifa
    /// calculada (o deja los cargos de origen vinculados) y queda pendiente de pago; sin monto (exento o sin
    /// tarifa) pasa al equipo que presta el servicio o se completa.
    /// </summary>
    public Task<Result> SettleAsync(
        ServiceRequest request,
        ServiceDefinition definition,
        ServiceActor actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.TotalAmount <= 0m)
        {
            var note = request.IsExempt ? $"Sin cobro: {request.ExemptionReference}." : null;
            return Task.FromResult(Fulfill(dbContext, request, definition, actor, note, now));
        }

        if (definition.PricingMode == ServicePricingModes.Tariff)
        {
            var quote = ReadQuote(request);
            var charge = new LocalCharge
            {
                BillOfLadingId = request.BillOfLadingId,
                ChargeType = request.ChargeConceptCode!,
                Description = $"{definition.NameEs} ({request.RequestNumber})",
                Amount = request.Amount,
                Currency = request.Currency!,
                Status = ChargeStatus.Pending,
                IsTaxable = request.TaxAmount > 0m,
                TaxRate = quote?.TaxRate ?? 0m,
                TaxAmount = request.TaxAmount,
                TotalAmount = request.TotalAmount
            };
            dbContext.LocalCharges.Add(charge);
            dbContext.ServiceRequestCharges.Add(new ServiceRequestCharge
            {
                ServiceRequestId = request.Id,
                LocalChargeId = charge.Id,
                Generated = true
            });
        }

        return Task.FromResult(Transition(dbContext, request, ServiceRequestStatus.PendingPayment, actor,
            $"Total {request.TotalAmount:0.##} {request.Currency}.", now));
    }

    /// <summary>Servicio sin cobro o ya pagado: en curso con el equipo que lo presta, o completado.</summary>
    public static Result Fulfill(
        IApplicationDbContext dbContext,
        ServiceRequest request,
        ServiceDefinition definition,
        ServiceActor actor,
        string? notes,
        DateTime now)
    {
        if (definition.FulfillmentTeam != ServiceTeams.None)
        {
            request.AssignedTeam = definition.FulfillmentTeam;
            return Transition(dbContext, request, ServiceRequestStatus.InProgress, actor, notes, now);
        }

        request.AssignedTeam = null;
        return Transition(dbContext, request, ServiceRequestStatus.Completed, actor, notes, now);
    }

    /// <summary>Anulación por el cliente antes del pago: el cargo generado se retira (y sale del carro).</summary>
    public async Task<Result> CancelAsync(
        ServiceRequest request,
        ServiceActor actor,
        string? reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!ServiceRequestStatus.Cancellable.Contains(request.Status))
            return Result.Failure(DomainErrors.ServiceRequest.InvalidTransition(request.Status, ServiceRequestStatus.Cancelled));

        var links = await dbContext.ServiceRequestCharges
            .Where(l => l.ServiceRequestId == request.Id)
            .ToListAsync(cancellationToken);
        var chargeIds = links.Select(l => l.LocalChargeId).ToList();

        if (chargeIds.Count > 0)
        {
            var paymentIds = await dbContext.PaymentDetails.AsNoTracking()
                .Where(d => d.ItemType == PayableItemTypes.LocalCharge && d.SourceId != null && chargeIds.Contains(d.SourceId.Value))
                .Select(d => d.PaymentId)
                .ToListAsync(cancellationToken);
            var statuses = await dbContext.Payments.AsNoTracking()
                .Where(p => paymentIds.Contains(p.Id))
                .Select(p => p.Status)
                .ToListAsync(cancellationToken);
            if (statuses.Any(s => s == PaymentStatus.Confirmed || Domain.Payments.PaymentStateMachine.IsInFlight(s)))
                return Result.Failure(DomainErrors.ServiceRequest.PaymentInProgress);

            var cartItems = await dbContext.CartItems
                .Where(i => i.ItemType == PayableItemTypes.LocalCharge && chargeIds.Contains(i.SourceId))
                .ToListAsync(cancellationToken);
            if (cartItems.Any(i => i.LockedByPaymentId is not null))
                return Result.Failure(DomainErrors.ServiceRequest.PaymentInProgress);
            foreach (var item in cartItems)
                dbContext.CartItems.Remove(item);

            // El cargo generado por la solicitud se retira; los del sistema de origen quedan como estaban.
            var generated = links.Where(l => l.Generated).Select(l => l.LocalChargeId).ToList();
            var charges = await dbContext.LocalCharges
                .Where(c => generated.Contains(c.Id))
                .ToListAsync(cancellationToken);
            foreach (var charge in charges)
            {
                if (paymentIds.Count > 0)
                {
                    charge.DeletedAt = now;
                    charge.DeletedBy = actor.Name;
                }
                else
                {
                    dbContext.LocalCharges.Remove(charge);
                }
            }
        }

        return Transition(dbContext, request, ServiceRequestStatus.Cancelled, actor, reason, now);
    }

    /// <summary>Aviso del estado alcanzado a los administradores de la organización solicitante (campana y correo).</summary>
    public async Task NotifyAsync(ServiceRequest request, string definitionName, CancellationToken cancellationToken)
    {
        var type = NotificationTypes.ForServiceRequestStatus(request.Status);
        if (type is null)
            return;

        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.OrganizationId, cancellationToken);
        if (organization is null)
            return;

        var (title, body) = Message(request, definitionName);
        await OrganizationNotifier.NotifyAdminsAsync(
            dbContext, notificationPublisher, organization, type, title, body, cancellationToken,
            dedupKeyPrefix: $"service-request:{request.Id}:{request.Status}",
            link: new NotificationLink(NotificationEntityTypes.ServiceRequest, request.Id.ToString(), request.RequestNumber, request.BlNumber),
            action: new NotificationAction(NotificationActionTypes.OpenServiceRequest, request.Id.ToString()));
    }

    public static (string Title, string Body) Message(ServiceRequest request, string definitionName)
    {
        var subject = $"{definitionName} {request.RequestNumber} (BL {request.BlNumber})";
        return request.Status switch
        {
            ServiceRequestStatus.PendingApproval => ($"Solicitud {request.RequestNumber} en revisión",
                $"La solicitud {subject} fue derivada al equipo {request.AssignedTeam} para su revisión."),
            ServiceRequestStatus.Approved => ($"Solicitud {request.RequestNumber} aprobada",
                $"La solicitud {subject} fue aprobada.{Notes(request)}"),
            ServiceRequestStatus.Rejected => ($"Solicitud {request.RequestNumber} rechazada",
                $"La solicitud {subject} fue rechazada.{Notes(request)}"),
            ServiceRequestStatus.PendingPayment => ($"Solicitud {request.RequestNumber} lista para pago",
                $"La solicitud {subject} quedó pendiente de pago por {request.TotalAmount:N2} {request.Currency}. Agréguela al carro de compra."),
            ServiceRequestStatus.Paid => ($"Solicitud {request.RequestNumber} pagada",
                $"El pago de la solicitud {subject} fue confirmado."),
            ServiceRequestStatus.InProgress => ($"Solicitud {request.RequestNumber} en curso",
                $"La solicitud {subject} está en curso con el equipo {request.AssignedTeam}."),
            ServiceRequestStatus.Completed => ($"Solicitud {request.RequestNumber} completada",
                $"La solicitud {subject} fue completada.{Notes(request)}"),
            ServiceRequestStatus.Cancelled => ($"Solicitud {request.RequestNumber} anulada",
                $"La solicitud {subject} fue anulada."),
            _ => ($"Solicitud {request.RequestNumber}", $"La solicitud {subject} cambió a {request.Status}.")
        };
    }

    public static ServiceQuoteDto? ReadQuote(ServiceRequest request) =>
        string.IsNullOrWhiteSpace(request.PricingDetailJson)
            ? null
            : JsonSerializer.Deserialize<ServiceQuoteDto>(request.PricingDetailJson, ServiceInputSchema.JsonOptions);

    private static void ApplyQuote(ServiceRequest request, ServiceQuoteDto quote)
    {
        request.ChargeConceptCode = quote.ChargeConceptCode;
        request.TariffId = quote.TariffId;
        request.TariffCode = quote.TariffCode;
        request.TariffSource = quote.TariffSource;
        request.TierUnit = quote.TierUnit;
        request.MeasuredUnits = quote.MeasuredUnits;
        request.Quantity = quote.Quantity;
        request.Timing = quote.Timing;
        request.MilestoneAt = quote.MilestoneAt;
        request.MilestoneSource = quote.MilestoneSource;
        request.Amount = quote.Amount;
        request.TaxAmount = quote.TaxAmount;
        request.TotalAmount = quote.TotalAmount;
        request.Currency = quote.Currency;
        request.IsExempt = quote.IsExempt;
        request.ExemptionReference = quote.ExemptionReference;
        request.QuotedAt = quote.QuotedAt;
        request.PricingDetailJson = JsonSerializer.Serialize(quote, ServiceInputSchema.JsonOptions);
    }

    private void ApplyExemptions(ServiceQuoteComputation computed, ServiceShipmentContext context, ServiceActor actor, DateTime now)
    {
        var charges = computed.SourceCharges.ToDictionary(c => c.Id);
        foreach (var exemption in computed.Exemptions)
        {
            if (!charges.TryGetValue(exemption.ChargeId, out var charge))
                continue;

            if (exemption.FullyExempt)
                charge.Status = ChargeStatus.Exempt;

            var trace = exemption.Trace;
            dbContext.AppliedExemptions.Add(new AppliedExemption
            {
                BillOfLadingId = context.BillOfLading.Id,
                LocalChargeId = charge.Id,
                ConceptCode = charge.ChargeType,
                ExemptParty = trace.Party,
                PartyTaxId = trace.TaxId,
                PartyMatchCode = trace.MatchCode,
                ExemptAmount = trace.ExemptAmount,
                Currency = charge.Currency,
                ConditionAmount = trace.ConditionAmount,
                ConditionCurrency = trace.ConditionCurrency,
                ConditionValidFrom = trace.ValidFrom,
                ConditionValidTo = trace.ValidTo,
                Source = trace.Source,
                PayerClientId = context.Organization.Id,
                AppliedByUserId = actor.UserId,
                AppliedAt = now
            });
        }
    }

    private static string Notes(ServiceRequest request) =>
        string.IsNullOrWhiteSpace(request.ResolutionNotes) ? string.Empty : $" Observación: {request.ResolutionNotes}";

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }

    private static bool IsEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MailAddress.TryCreate(value.Trim(), out var address) && address.Address == value.Trim();
}
