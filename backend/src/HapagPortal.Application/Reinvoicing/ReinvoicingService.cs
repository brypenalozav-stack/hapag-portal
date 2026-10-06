namespace HapagPortal.Application.Reinvoicing;

using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using Microsoft.EntityFrameworkCore;

/// <summary>Cargo de la refacturación: el de refacturar (tarifa M8-01 + IVA) o la pérdida de IVA (sin IVA).</summary>
public sealed record ReinvoicingChargeDto(
    string ConceptCode,
    string ConceptName,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string? TariffCode);

/// <summary>
/// Cobro de la refacturación IAO de una factura si se enviara ahora (M3-11): cargo por refacturar con la tarifa
/// vigente y pérdida de IVA (el impuesto de la factura original, convertido a la moneda del cobro con el tipo de
/// Nexus del día cuando corresponde, M5-05). Ambos se pagan juntos en el mismo módulo.
/// </summary>
public sealed record ReinvoicingQuoteDto(
    Guid InvoiceId,
    string? SiiNumber,
    string SourceNumber,
    string LegalName,
    string TaxId,
    decimal InvoiceTotal,
    decimal InvoiceTaxAmount,
    string InvoiceCurrency,
    bool Eligible,
    string? IneligibleReason,
    ReinvoicingChargeDto? Fee,
    ReinvoicingChargeDto? VatLoss,
    decimal TotalAmount,
    string? Currency,
    decimal? ExchangeRate,
    DateTime QuotedAt,
    string TimeZone);

/// <summary>Cálculo interno con los valores que se fijan al enviar.</summary>
public sealed record ReinvoicingComputation(
    ReinvoicingChargeDto Fee,
    ReinvoicingChargeDto? VatLoss,
    decimal TaxRate,
    decimal? ExchangeRate,
    ResolvedTariff Tariff);

/// <summary>
/// Refacturación IAO con pérdida de IVA (M3-11) sobre el modelo de servicios on demand (Ola G): una solicitud de
/// la definición <c>IAO_REINVOICING</c> por factura, con los nuevos datos de facturación, la aprobación de la nueva
/// razón social adjunta, el cobro (refacturación + pérdida de IVA) como cargos locales del BL pagados por el carro
/// o la vista de crédito, la aceptación del cobro por la nueva razón social por un enlace de un solo uso enviado a
/// su correo, y la emisión de la nueva factura por <see cref="IInvoiceProvider"/> solo con pago y aceptación. La
/// factura original queda reemplazada (M7-01). No guarda: lo hace el llamador.
/// </summary>
public sealed class ReinvoicingService(
    IApplicationDbContext dbContext,
    ITariffResolver tariffResolver,
    IExchangeRateService exchangeRateService,
    IEmailService emailService,
    IInvoiceProvider invoiceProvider,
    INotificationPublisher notificationPublisher)
{
    /// <summary>Vigencia del enlace de aceptación.</summary>
    public static readonly TimeSpan AcceptanceValidity = TimeSpan.FromDays(15);

    public const string AcceptancePath = "/reinvoicing/acceptance/";
    public const string SourceSystem = "PORTAL";

    private static readonly string[] EligibleDocumentTypes = [InvoiceDocumentTypes.Invoice, InvoiceDocumentTypes.ExemptInvoice];

    /// <summary>Motivo por el que la factura no admite refacturación (nulo si la admite).</summary>
    public async Task<Error?> IneligibilityAsync(CustomerInvoice invoice, Guid? excludeRequestId, CancellationToken cancellationToken)
    {
        if (invoice.Country != CountryCodes.Chile
            || invoice.SiiNumber is null
            || invoice.BillOfLadingId is null
            || !EligibleDocumentTypes.Contains(invoice.DocumentType)
            || invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Superseded)
        {
            return DomainErrors.Reinvoicing.InvoiceNotEligible;
        }

        var requestIds = await dbContext.InvoiceReissues.AsNoTracking()
            .Where(r => r.OriginalInvoiceId == invoice.Id)
            .Select(r => r.ServiceRequestId)
            .ToListAsync(cancellationToken);
        var active = await dbContext.ServiceRequests.AsNoTracking()
            .AnyAsync(r => requestIds.Contains(r.Id) && r.Id != excludeRequestId && !ServiceRequestStatus.Released.Contains(r.Status), cancellationToken);

        return active ? DomainErrors.Reinvoicing.AlreadyRequested : null;
    }

    /// <summary>Cobro en el instante indicado: tarifa vigente (NF-22) e impuesto del país; pérdida de IVA convertida.</summary>
    public async Task<Result<ReinvoicingComputation>> ComputeAsync(
        ServiceDefinition definition,
        CustomerInvoice invoice,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var country = invoice.Country;
        var today = BusinessCalendar.LocalDate(country, now);
        var concept = definition.ChargeConceptCode ?? ChargeConceptCodes.Reinvoicing;

        var tariff = (await tariffResolver.GetInForceAsync(new TariffLookup(country, concept, today, null, definition.TariffCode), cancellationToken))
            .FirstOrDefault();
        if (tariff is null)
            return Result<ReinvoicingComputation>.Failure(DomainErrors.Tariff.NotInForce(concept, country));

        var currency = tariff.Currency;
        var taxRate = definition.Taxable
            ? await dbContext.TaxConfigurations.AsNoTracking()
                .Where(t => t.Country == country && t.IsActive)
                .Select(t => (decimal?)t.TaxRate)
                .FirstOrDefaultAsync(cancellationToken) ?? 0m
            : 0m;
        var feeAmount = MoneyRounding.Round(tariff.Amount, currency);
        var feeTax = MoneyRounding.Round(feeAmount * taxRate / 100m, currency);
        var names = await dbContext.ChargeConcepts.AsNoTracking()
            .Where(c => c.Code == concept || c.Code == ChargeConceptCodes.VatLoss)
            .ToDictionaryAsync(c => c.Code, c => c.Name, cancellationToken);

        var fee = new ReinvoicingChargeDto(concept, names.GetValueOrDefault(concept) ?? "Refacturación", feeAmount, feeTax, feeAmount + feeTax,
            currency, tariff.Code);

        ReinvoicingChargeDto? vatLoss = null;
        decimal? rate = null;
        if (invoice.TaxAmount > 0m)
        {
            var quote = await exchangeRateService.GetQuoteAsync(invoice.Currency, currency, today, cancellationToken);
            if (quote.IsFailure)
                return Result<ReinvoicingComputation>.Failure(quote.Error);

            rate = invoice.Currency == currency ? null : quote.Value.Rate;
            var loss = MoneyRounding.Round(invoice.TaxAmount * quote.Value.Rate, currency);
            vatLoss = new ReinvoicingChargeDto(ChargeConceptCodes.VatLoss, names.GetValueOrDefault(ChargeConceptCodes.VatLoss) ?? "Pérdida de IVA",
                loss, 0m, loss, currency, null);
        }

        return Result<ReinvoicingComputation>.Success(new ReinvoicingComputation(fee, vatLoss, taxRate, rate, tariff));
    }

    public static ReinvoicingQuoteDto ToQuote(CustomerInvoice invoice, Error? ineligible, ReinvoicingComputation? computation, DateTime now) => new(
        invoice.Id,
        invoice.SiiNumber,
        invoice.SourceNumber,
        invoice.LegalName,
        TaxIdNormalizer.Normalize(invoice.TaxId),
        invoice.TotalAmount,
        invoice.TaxAmount,
        invoice.Currency,
        ineligible is null && computation is not null,
        ineligible?.Code,
        computation?.Fee,
        computation?.VatLoss,
        computation is null ? 0m : computation.Fee.TotalAmount + (computation.VatLoss?.TotalAmount ?? 0m),
        computation?.Fee.Currency,
        computation?.ExchangeRate,
        now,
        BusinessCalendar.TimeZoneId(invoice.Country));

    /// <summary>
    /// Envía la solicitud: aprobación de la nueva razón social adjunta, datos de facturación completos y distintos
    /// de los facturados, tarifa aceptada (y sin cambios), cargos de refacturación y pérdida de IVA generados en el
    /// BL y enlace de aceptación enviado a la nueva razón social. La solicitud queda pendiente de pago.
    /// </summary>
    public async Task<Result> SubmitAsync(
        ServiceRequest request,
        InvoiceReissue reissue,
        ServiceDefinition definition,
        CustomerInvoice invoice,
        ServiceActor actor,
        bool acceptTariff,
        decimal? acceptedTotal,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Status != ServiceRequestStatus.Draft)
            return Result.Failure(DomainErrors.ServiceRequest.InvalidTransition(request.Status, ServiceRequestStatus.Submitted));

        var ineligible = await IneligibilityAsync(invoice, request.Id, cancellationToken);
        if (ineligible is not null)
            return Result.Failure(ineligible);

        var billing = ValidateBilling(request.BillingTaxId, request.BillingName, request.BillingAddress, request.BillingEmail, invoice);
        if (billing.IsFailure)
            return billing;

        var approved = await dbContext.ServiceRequestAttachments.AsNoTracking()
            .AnyAsync(a => a.ServiceRequestId == request.Id && a.FieldKey == ReinvoicingFields.Approval, cancellationToken);
        if (!approved)
            return Result.Failure(DomainErrors.Reinvoicing.ApprovalRequired);

        var computed = await ComputeAsync(definition, invoice, now, cancellationToken);
        if (computed.IsFailure)
            return computed;

        var computation = computed.Value;
        var fee = computation.Fee;
        var total = fee.TotalAmount + (computation.VatLoss?.TotalAmount ?? 0m);
        if (!acceptTariff)
            return Result.Failure(DomainErrors.ServiceRequest.TariffNotAccepted);
        if (acceptedTotal is not null && acceptedTotal.Value != total)
            return Result.Failure(DomainErrors.ServiceRequest.TariffChanged(total, fee.Currency));

        // Cobro fijado al enviar (NF-22) y visible en la solicitud.
        var lines = new List<ServiceQuoteLineDto> { new(null, null, fee.Amount, fee.TariffCode, []) };
        if (computation.VatLoss is { } loss)
            lines.Add(new ServiceQuoteLineDto(null, null, loss.Amount, ChargeConceptCodes.VatLoss, []));

        var quote = new ServiceQuoteDto(
            definition.PricingMode, fee.ConceptCode, total > 0m, fee.Amount + (computation.VatLoss?.Amount ?? 0m), fee.TaxAmount, total,
            fee.Currency, computation.TaxRate, 1, null, null, ServiceTimings.NotApplicable, null, null, computation.Tariff.TariffId,
            computation.Tariff.Code, computation.Tariff.Source, false, null, [], lines, [], BusinessCalendar.TimeZoneId(invoice.Country), now);

        request.ChargeConceptCode = fee.ConceptCode;
        request.TariffId = computation.Tariff.TariffId;
        request.TariffCode = computation.Tariff.Code;
        request.TariffSource = computation.Tariff.Source;
        request.Quantity = 1;
        request.Timing = ServiceTimings.NotApplicable;
        request.Amount = quote.Amount;
        request.TaxAmount = quote.TaxAmount;
        request.TotalAmount = quote.TotalAmount;
        request.Currency = quote.Currency;
        request.QuotedAt = now;
        request.TariffAcceptedAt = now;
        request.PricingDetailJson = JsonSerializer.Serialize(quote, ServiceInputSchema.JsonOptions);

        reissue.FeeAmount = fee.Amount;
        reissue.FeeTaxAmount = fee.TaxAmount;
        reissue.VatLossAmount = computation.VatLoss?.Amount ?? 0m;
        reissue.Currency = fee.Currency;
        reissue.ExchangeRate = computation.ExchangeRate;

        var number = invoice.SiiNumber ?? invoice.SourceNumber;
        AddCharge(request, fee.ConceptCode, $"Refacturación de la factura {number} ({request.RequestNumber})", fee.Amount, fee.TaxAmount,
            computation.TaxRate, fee.Currency);
        if (computation.VatLoss is { } vat)
            AddCharge(request, vat.ConceptCode, $"Pérdida de IVA de la factura {number} ({request.RequestNumber})", vat.Amount, 0m, 0m, vat.Currency);

        var submitted = ServiceRequestWorkflow.Transition(dbContext, request, ServiceRequestStatus.Submitted, actor, null, now);
        if (submitted.IsFailure)
            return submitted;

        var pending = ServiceRequestWorkflow.Transition(dbContext, request, ServiceRequestStatus.PendingPayment, ServiceActor.System,
            $"Total {total:0.##} {fee.Currency}. La factura se emite con el pago y la aceptación de la nueva razón social.", now);
        if (pending.IsFailure)
            return pending;

        await RequestAcceptanceAsync(request, reissue, invoice, now, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Genera un enlace de aceptación nuevo (el anterior deja de servir) y lo envía al correo de la nueva razón
    /// social. Solo se guarda el SHA-256 del token.
    /// </summary>
    public async Task<string> RequestAcceptanceAsync(
        ServiceRequest request,
        InvoiceReissue reissue,
        CustomerInvoice invoice,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var token = NewToken();
        reissue.AcceptanceStatus = ReinvoicingAcceptanceStatus.Pending;
        reissue.AcceptanceTokenHash = Hash(token);
        reissue.AcceptanceRequestedAt = now;
        reissue.AcceptanceExpiresAt = now + AcceptanceValidity;

        var number = invoice.SiiNumber ?? invoice.SourceNumber;
        var body = new StringBuilder()
            .AppendLine($"{request.BillingName} ({request.BillingTaxId}):")
            .AppendLine()
            .AppendLine($"{invoice.LegalName} solicitó a Hapag-Lloyd refacturar a su nombre la factura {number} ({invoice.SourceNumber}).")
            .AppendLine($"Cobro de la refacturación: {request.TotalAmount.ToString("N2", CultureInfo.InvariantCulture)} {request.Currency} " +
                $"(refacturación {reissue.FeeAmount + reissue.FeeTaxAmount:N2} y pérdida de IVA {reissue.VatLossAmount:N2}).")
            .AppendLine("La nueva factura solo se emite con su aceptación. Para aceptar o rechazar, abra en el Portal de Clientes:")
            .AppendLine($"{AcceptancePath}{token}")
            .AppendLine()
            .AppendLine($"El enlace es personal, de un solo uso y vence el {BusinessCalendar.ToLocal(invoice.Country, reissue.AcceptanceExpiresAt.Value):dd-MM-yyyy HH:mm} ({BusinessCalendar.TimeZoneId(invoice.Country)}).")
            .ToString();

        await emailService.SendEmailAsync(
            reissue.AcceptorEmail,
            $"Hapag-Lloyd - Aceptación de refacturación {request.RequestNumber}",
            body,
            cancellationToken);

        return token;
    }

    /// <summary>
    /// Emite la nueva factura (requiere pago y aceptación), la registra en M7-01 a nombre de la nueva razón social
    /// (en la organización del portal con ese RUT o, si no existe, en la que solicitó), reemplaza la original y
    /// completa la solicitud. Un rechazo o una falla de la fuente lanza para que la cola reintente (NF-03).
    /// </summary>
    public async Task<CustomerInvoice> IssueAsync(ServiceRequest request, InvoiceReissue reissue, DateTime now, CancellationToken cancellationToken)
    {
        var original = await dbContext.CustomerInvoices.FirstOrDefaultAsync(i => i.Id == reissue.OriginalInvoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"The invoice '{reissue.OriginalInvoiceId}' of {request.RequestNumber} no longer exists.");

        var exempt = original.DocumentType == InvoiceDocumentTypes.ExemptInvoice;
        var vatRate = original.NetAmount > 0m && original.TaxAmount > 0m ? Math.Round(original.TaxAmount * 100m / original.NetAmount, 2) : (decimal?)null;
        var issueDate = BusinessCalendar.LocalDate(original.Country, now);

        var issued = await invoiceProvider.IssueAsync(
            new InvoiceIssueRequest(
                exempt ? 34 : 33,
                request.RequestNumber,
                issueDate,
                original.Currency,
                null,
                new InvoiceReceiver(request.BillingTaxId!, request.BillingName!, request.BillingActivity, request.BillingAddress, null, null, request.BillingEmail),
                [new InvoiceLine(1, original.ConceptCode, $"Refacturación de la factura {original.SiiNumber ?? original.SourceNumber} ({original.SourceNumber})",
                    1m, original.NetAmount, exempt, original.NetAmount)],
                new InvoiceTotals(exempt ? 0m : original.NetAmount, exempt ? original.NetAmount : 0m, vatRate, original.TaxAmount, original.TotalAmount)),
            cancellationToken);
        if (issued.IsFailure)
            throw new InvalidOperationException($"{issued.Error.Code}: {issued.Error.Message}");
        if (string.Equals(issued.Value.Status, "REJECTED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"The new invoice of {request.RequestNumber} was rejected by the tax authority (folio {issued.Value.Folio}).");

        var newTaxId = TaxIdNormalizer.Normalize(request.BillingTaxId);
        var organizations = await dbContext.Clients.AsNoTracking().Select(c => new { c.Id, c.TaxId }).ToListAsync(cancellationToken);
        var owner = organizations.FirstOrDefault(c => TaxIdNormalizer.AreEqual(c.TaxId, newTaxId))?.Id ?? original.OrganizationId;
        var paid = original.Status == InvoiceStatus.Paid;

        var replacement = new CustomerInvoice
        {
            OrganizationId = owner,
            SiiNumber = issued.Value.Folio,
            SourceNumber = request.RequestNumber,
            DocumentType = original.DocumentType,
            IssueDate = issueDate,
            DueDate = original.DueDate,
            BillOfLadingId = original.BillOfLadingId,
            BlNumber = original.BlNumber,
            BookingNumber = original.BookingNumber,
            LegalName = request.BillingName!,
            TaxId = newTaxId,
            NetAmount = original.NetAmount,
            TaxAmount = original.TaxAmount,
            TotalAmount = original.TotalAmount,
            Currency = original.Currency,
            Status = paid ? InvoiceStatus.Paid : InvoiceStatus.Pending,
            SiiStatus = issued.Value.Status,
            IsPayable = !paid && original.IsPayable,
            Country = original.Country,
            ConceptCode = original.ConceptCode,
            PaidAt = original.PaidAt,
            PaymentId = original.PaymentId,
            SyncedAt = now,
            Source = SourceSystem,
            SupersedesInvoiceId = original.Id,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        };
        dbContext.CustomerInvoices.Add(replacement);

        original.Status = InvoiceStatus.Superseded;
        original.SupersededByInvoiceId = replacement.Id;
        original.IsPayable = false;

        // La factura reemplazada sale de los carros (no se puede pagar).
        var stale = await dbContext.CartItems
            .Where(i => i.ItemType == PayableItemTypes.Invoice && i.SourceId == original.Id && i.LockedByPaymentId == null)
            .ToListAsync(cancellationToken);
        foreach (var item in stale)
            dbContext.CartItems.Remove(item);

        reissue.NewInvoiceId = replacement.Id;
        reissue.IssuedAt = now;

        var completed = ServiceRequestWorkflow.Transition(dbContext, request, ServiceRequestStatus.Completed, ServiceActor.System,
            $"Factura {issued.Value.Folio} emitida a {request.BillingName} ({newTaxId}); reemplaza la factura {original.SiiNumber ?? original.SourceNumber}.", now);
        if (completed.IsFailure)
            throw new InvalidOperationException(completed.Error.Message);
        request.AssignedTeam = null;
        request.ResolutionNotes = $"Factura {issued.Value.Folio} emitida a {request.BillingName} ({newTaxId}).";

        return replacement;
    }

    /// <summary>Aviso del estado de la refacturación a los administradores de la organización solicitante.</summary>
    public async Task NotifyAsync(ServiceRequest request, string type, string title, string body, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.OrganizationId, cancellationToken);
        if (organization is null)
            return;

        await Organizations.Common.OrganizationNotifier.NotifyAdminsAsync(
            dbContext, notificationPublisher, organization, type, title, body, cancellationToken,
            dedupKeyPrefix: $"reinvoicing:{request.Id}:{type}");
    }

    /// <summary>Datos de la nueva razón social: RUT, razón social, dirección y correo válidos, RUT distinto del facturado.</summary>
    public static Result ValidateBilling(string? taxId, string? name, string? address, string? email, CustomerInvoice invoice)
    {
        if (string.IsNullOrWhiteSpace(taxId) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address) || !IsEmail(email))
            return Result.Failure(DomainErrors.ServiceRequest.BillingDataRequired);

        return TaxIdNormalizer.AreEqual(taxId, invoice.TaxId)
            ? Result.Failure(DomainErrors.Reinvoicing.SameTaxId)
            : Result.Success();
    }

    public static bool IsEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MailAddress.TryCreate(value.Trim(), out var address) && address.Address == value.Trim();

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();

    private static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private void AddCharge(ServiceRequest request, string concept, string description, decimal amount, decimal tax, decimal taxRate, string currency)
    {
        var charge = new LocalCharge
        {
            BillOfLadingId = request.BillOfLadingId,
            ChargeType = concept,
            Description = description,
            Amount = amount,
            Currency = currency,
            Status = ChargeStatus.Pending,
            IsTaxable = tax > 0m,
            TaxRate = taxRate,
            TaxAmount = tax,
            TotalAmount = amount + tax
        };
        dbContext.LocalCharges.Add(charge);
        dbContext.ServiceRequestCharges.Add(new ServiceRequestCharge
        {
            ServiceRequestId = request.Id,
            LocalChargeId = charge.Id,
            Generated = true
        });
    }
}
