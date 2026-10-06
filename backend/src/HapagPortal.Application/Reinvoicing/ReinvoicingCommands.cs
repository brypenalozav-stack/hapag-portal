namespace HapagPortal.Application.Reinvoicing;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Datos propios de la refacturación (M3-11): factura original, cobro, aceptación y nueva factura.</summary>
public sealed record InvoiceReissueDto(
    Guid Id,
    Guid OriginalInvoiceId,
    string? OriginalSiiNumber,
    string OriginalSourceNumber,
    string OriginalLegalName,
    string OriginalTaxId,
    string? NewLegalName,
    string? NewTaxId,
    decimal VatLossAmount,
    decimal FeeAmount,
    decimal FeeTaxAmount,
    string? Currency,
    decimal? ExchangeRate,
    string AcceptorEmail,
    string? AcceptanceStatus,
    DateTime? AcceptanceRequestedAt,
    DateTime? AcceptanceExpiresAt,
    DateTime? AcceptedAt,
    string? AcceptedByName,
    string? AcceptedByTaxId,
    DateTime? DeclinedAt,
    string? DeclineReason,
    Guid? NewInvoiceId,
    string? NewInvoiceNumber,
    DateTime? IssuedAt,
    bool ApprovalAttached,
    bool CanResendAcceptance);

/// <summary>Solicitud de refacturación: la solicitud genérica (estado, cargos, adjuntos, línea de tiempo) y sus datos propios.</summary>
public sealed record ReinvoicingDetailDto(ServiceRequestDetailDto Request, InvoiceReissueDto Reissue);

/// <summary>Lo que ve la nueva razón social en el enlace de aceptación (sin datos de otras operaciones).</summary>
public sealed record ReinvoicingAcceptanceViewDto(
    string RequestNumber,
    string OriginalInvoiceNumber,
    string OriginalLegalName,
    string OriginalTaxId,
    decimal OriginalTotal,
    string OriginalCurrency,
    string? NewLegalName,
    string? NewTaxId,
    decimal FeeTotal,
    decimal VatLossAmount,
    decimal TotalAmount,
    string? Currency,
    string AcceptanceStatus,
    DateTime? ExpiresAt,
    DateTime? AcceptedAt,
    DateTime? DeclinedAt,
    string TimeZone);

/// <summary>Cobro estimado de refacturar la factura (y si es elegible).</summary>
public sealed record GetReinvoicingQuoteQuery(Guid InvoiceId) : IQuery<ReinvoicingQuoteDto>;

/// <summary>
/// Crea el borrador de refacturación IAO de una factura propia (M3-11) con los datos de la nueva razón social. Luego
/// se adjunta su aprobación (<c>POST /service-requests/{id}/attachments</c>, campo <c>newCompanyApproval</c>) y se envía.
/// </summary>
public sealed record CreateReinvoicingCommand(
    Guid InvoiceId,
    ServiceBillingInput Billing,
    string? AcceptorEmail,
    string? Reason) : ICommand<ReinvoicingDetailDto>;

/// <summary>Cambia los datos de un borrador (nulo = se conserva).</summary>
public sealed record UpdateReinvoicingCommand(
    Guid Id,
    ServiceBillingInput? Billing,
    string? AcceptorEmail,
    string? Reason) : ICommand<ReinvoicingDetailDto>;

/// <summary>Envía la solicitud: genera los cargos y el enlace de aceptación para la nueva razón social.</summary>
public sealed record SubmitReinvoicingCommand(Guid Id, bool AcceptTariff, decimal? AcceptedTotal) : ICommand<ReinvoicingDetailDto>;

/// <summary>Reenvía el enlace de aceptación (uno nuevo; el anterior deja de servir) mientras esté pendiente.</summary>
public sealed record ResendReinvoicingAcceptanceCommand(Guid Id) : ICommand<ReinvoicingDetailDto>;

public sealed record GetReinvoicingQuery(Guid Id) : IQuery<ReinvoicingDetailDto>;

/// <summary>Consulta pública del enlace de aceptación (sin sesión: el token es la credencial).</summary>
public sealed record GetReinvoicingAcceptanceQuery(string Token) : IQuery<ReinvoicingAcceptanceViewDto>;

/// <summary>
/// Respuesta de la nueva razón social (sin sesión): acepta o rechaza el cobro declarando el nombre y RUT de quien
/// responde; queda registrado con fecha, hora y dirección de origen. Aceptado y pagado, se encola la emisión.
/// </summary>
public sealed record RespondReinvoicingAcceptanceCommand(
    string Token,
    bool Accept,
    string? Name,
    string? TaxId,
    string? Reason,
    string? RemoteAddress) : ICommand<ReinvoicingAcceptanceViewDto>;

public sealed class CreateReinvoicingCommandValidator : AbstractValidator<CreateReinvoicingCommand>
{
    public CreateReinvoicingCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Billing).NotNull().SetValidator(new ServiceBillingInputValidator());
        RuleFor(x => x.AcceptorEmail).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.AcceptorEmail));
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class UpdateReinvoicingCommandValidator : AbstractValidator<UpdateReinvoicingCommand>
{
    public UpdateReinvoicingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Billing!).SetValidator(new ServiceBillingInputValidator()).When(x => x.Billing is not null);
        RuleFor(x => x.AcceptorEmail).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.AcceptorEmail));
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class SubmitReinvoicingCommandValidator : AbstractValidator<SubmitReinvoicingCommand>
{
    public SubmitReinvoicingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AcceptedTotal).GreaterThanOrEqualTo(0m).When(x => x.AcceptedTotal is not null);
    }
}

public sealed class RespondReinvoicingAcceptanceCommandValidator : AbstractValidator<RespondReinvoicingAcceptanceCommand>
{
    public RespondReinvoicingAcceptanceCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.TaxId).MaximumLength(20);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

/// <summary>Carga de la refacturación de la propia organización y su vista.</summary>
public static class ReinvoicingViews
{
    public static async Task<Result<(ServiceRequest Request, InvoiceReissue Reissue, AccessScope Scope)>> LoadAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        Guid id,
        bool track,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (!scope.IsOperational || scope.OrganizationId is null)
            return Result<(ServiceRequest, InvoiceReissue, AccessScope)>.Failure(Error.Forbidden);

        var organizationId = scope.OrganizationId.Value;
        var requests = track ? dbContext.ServiceRequests : dbContext.ServiceRequests.AsNoTracking();
        var request = await requests.FirstOrDefaultAsync(
            r => r.Id == id && r.DefinitionCode == ServiceDefinitionCodes.IaoReinvoicing
                && (r.OrganizationId == organizationId || r.OnBehalfOfClientId == organizationId),
            cancellationToken);
        var reissues = track ? dbContext.InvoiceReissues : dbContext.InvoiceReissues.AsNoTracking();
        var reissue = request is null ? null : await reissues.FirstOrDefaultAsync(r => r.ServiceRequestId == request.Id, cancellationToken);

        return request is null || reissue is null
            ? Result<(ServiceRequest, InvoiceReissue, AccessScope)>.Failure(DomainErrors.Reinvoicing.NotFound(id))
            : Result<(ServiceRequest, InvoiceReissue, AccessScope)>.Success((request, reissue, scope));
    }

    public static async Task<ReinvoicingDetailDto> DetailAsync(
        IApplicationDbContext dbContext,
        ServiceRequest request,
        InvoiceReissue reissue,
        bool clientView,
        CancellationToken cancellationToken)
    {
        var detail = await ServiceRequestViews.DetailAsync(dbContext, request, clientView, cancellationToken);
        var approval = await dbContext.ServiceRequestAttachments.AsNoTracking()
            .AnyAsync(a => a.ServiceRequestId == request.Id && a.FieldKey == ReinvoicingFields.Approval, cancellationToken);
        var newInvoice = reissue.NewInvoiceId is { } invoiceId
            ? await dbContext.CustomerInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            : null;

        return new ReinvoicingDetailDto(detail, new InvoiceReissueDto(
            reissue.Id, reissue.OriginalInvoiceId, reissue.OriginalSiiNumber, reissue.OriginalSourceNumber, reissue.OriginalLegalName,
            reissue.OriginalTaxId, request.BillingName, request.BillingTaxId, reissue.VatLossAmount, reissue.FeeAmount, reissue.FeeTaxAmount,
            reissue.Currency, reissue.ExchangeRate, reissue.AcceptorEmail, reissue.AcceptanceStatus, reissue.AcceptanceRequestedAt,
            reissue.AcceptanceExpiresAt, reissue.AcceptedAt, reissue.AcceptedByName, reissue.AcceptedByTaxId, reissue.DeclinedAt,
            reissue.DeclineReason, reissue.NewInvoiceId, newInvoice?.SiiNumber ?? newInvoice?.SourceNumber, reissue.IssuedAt, approval,
            CanResendAcceptance: clientView && reissue.AcceptanceStatus == ReinvoicingAcceptanceStatus.Pending
                && !ServiceRequestStatus.Terminal.Contains(request.Status)));
    }

    public static string InputJson(string invoiceNumber, string? reason) =>
        System.Text.Json.JsonSerializer.Serialize(
            new Dictionary<string, string?> { [ReinvoicingFields.InvoiceNumber] = invoiceNumber, [ReinvoicingFields.Reason] = reason },
            Domain.ServiceRequests.ServiceInputSchema.JsonOptions);

    public static string? ReadReason(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.TryGetProperty(ReinvoicingFields.Reason, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString()
            : null;
    }
}

public sealed class GetReinvoicingQuoteQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ReinvoicingService service)
    : IQueryHandler<GetReinvoicingQuoteQuery, ReinvoicingQuoteDto>
{
    public async Task<Result<ReinvoicingQuoteDto>> Handle(GetReinvoicingQuoteQuery request, CancellationToken cancellationToken)
    {
        var invoice = await InvoiceView.VisibleAsync(dbContext, accessEvaluator, request.InvoiceId, cancellationToken);
        if (invoice.IsFailure)
            return Result<ReinvoicingQuoteDto>.Failure(invoice.Error);

        var definition = await dbContext.ServiceDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == ServiceDefinitionCodes.IaoReinvoicing && d.IsActive, cancellationToken);
        if (definition is null)
            return Result<ReinvoicingQuoteDto>.Failure(DomainErrors.ServiceDefinition.NotFoundByCode(ServiceDefinitionCodes.IaoReinvoicing));

        var now = DateTime.UtcNow;
        var ineligible = await service.IneligibilityAsync(invoice.Value, null, cancellationToken);
        var computed = await service.ComputeAsync(definition, invoice.Value, now, cancellationToken);
        if (computed.IsFailure && ineligible is null)
            return Result<ReinvoicingQuoteDto>.Failure(computed.Error);

        return Result<ReinvoicingQuoteDto>.Success(
            ReinvoicingService.ToQuote(invoice.Value, ineligible, computed.IsSuccess ? computed.Value : null, now));
    }
}

public sealed class CreateReinvoicingCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    ReinvoicingService service)
    : ICommandHandler<CreateReinvoicingCommand, ReinvoicingDetailDto>
{
    public async Task<Result<ReinvoicingDetailDto>> Handle(CreateReinvoicingCommand request, CancellationToken cancellationToken)
    {
        var visible = await InvoiceView.VisibleAsync(dbContext, accessEvaluator, request.InvoiceId, cancellationToken);
        if (visible.IsFailure)
            return Result<ReinvoicingDetailDto>.Failure(visible.Error);

        var invoice = visible.Value;
        var ineligible = await service.IneligibilityAsync(invoice, null, cancellationToken);
        if (ineligible is not null)
            return Result<ReinvoicingDetailDto>.Failure(ineligible);

        var context = await ServiceShipmentLoader.LoadByIdAsync(dbContext, accessEvaluator, invoice.BillOfLadingId!.Value, cancellationToken);
        if (context.IsFailure)
            return Result<ReinvoicingDetailDto>.Failure(context.Error);

        var (bl, permissions, organization, scope) = context.Value;
        var definition = await dbContext.ServiceDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == ServiceDefinitionCodes.IaoReinvoicing && d.IsActive, cancellationToken);
        if (definition is null || !ServiceCatalogEvaluator.Applies(definition, bl, permissions))
            return Result<ReinvoicingDetailDto>.Failure(DomainErrors.ServiceDefinition.NotFoundByCode(ServiceDefinitionCodes.IaoReinvoicing));

        // Refactura la organización facturada (operación propia, M1-02 / M1-11).
        if (invoice.OrganizationId != organization.Id || !scope.CanOperate || !permissions.CanExecute(definition.ActionCode))
            return Result<ReinvoicingDetailDto>.Failure(Error.Forbidden);

        var billing = request.Billing;
        if (!string.IsNullOrWhiteSpace(billing.TaxId) && TaxIdNormalizer.AreEqual(billing.TaxId, invoice.TaxId))
            return Result<ReinvoicingDetailDto>.Failure(DomainErrors.Reinvoicing.SameTaxId);

        var now = DateTime.UtcNow;
        var number = invoice.SiiNumber ?? invoice.SourceNumber;
        var serviceRequest = new ServiceRequest
        {
            RequestNumber = PaymentLifecycle.NewNumber(DocumentPrefixes.ServiceRequest, now),
            DefinitionId = definition.Id,
            DefinitionCode = definition.Code,
            OrganizationId = organization.Id,
            RequestedByUserId = currentUserService.UserId,
            RequestedByEmail = currentUserService.Email,
            BillOfLadingId = bl.Id,
            BlNumber = bl.BLNumber,
            BookingNumber = bl.BookingNumber,
            Country = bl.Country,
            Operation = ServiceOperations.Of(bl.ShipmentType),
            InputValuesJson = ReinvoicingViews.InputJson(number, string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()),
            Status = ServiceRequestStatus.Draft,
            StatusChangedAt = now,
            CreatedAt = now
        };
        ServiceRequestWorkflow.WriteBilling(serviceRequest, billing);

        var reissue = new InvoiceReissue
        {
            ServiceRequestId = serviceRequest.Id,
            OriginalInvoiceId = invoice.Id,
            OriginalSiiNumber = invoice.SiiNumber,
            OriginalSourceNumber = invoice.SourceNumber,
            OriginalTaxId = TaxIdNormalizer.Normalize(invoice.TaxId),
            OriginalLegalName = invoice.LegalName,
            AcceptorEmail = (string.IsNullOrWhiteSpace(request.AcceptorEmail) ? billing.Email : request.AcceptorEmail)?.Trim() ?? string.Empty
        };

        dbContext.ServiceRequests.Add(serviceRequest);
        dbContext.InvoiceReissues.Add(reissue);
        ServiceRequestWorkflow.AddEvent(dbContext, serviceRequest, null, ServiceRequestStatus.Draft, ServiceActor.Client(currentUserService),
            $"Refacturación de la factura {number}.", now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ReinvoicingDetailDto>.Success(
            await ReinvoicingViews.DetailAsync(dbContext, serviceRequest, reissue, clientView: true, cancellationToken));
    }
}

public sealed class UpdateReinvoicingCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<UpdateReinvoicingCommand, ReinvoicingDetailDto>
{
    public async Task<Result<ReinvoicingDetailDto>> Handle(UpdateReinvoicingCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ReinvoicingViews.LoadAsync(dbContext, accessEvaluator, request.Id, track: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ReinvoicingDetailDto>.Failure(loaded.Error);

        var (serviceRequest, reissue, scope) = loaded.Value;
        if (serviceRequest.OrganizationId != scope.OrganizationId || !scope.CanOperate)
            return Result<ReinvoicingDetailDto>.Failure(Error.Forbidden);
        if (serviceRequest.Status != ServiceRequestStatus.Draft)
            return Result<ReinvoicingDetailDto>.Failure(DomainErrors.ServiceRequest.NotEditable);

        if (request.Billing is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Billing.TaxId) && TaxIdNormalizer.AreEqual(request.Billing.TaxId, reissue.OriginalTaxId))
                return Result<ReinvoicingDetailDto>.Failure(DomainErrors.Reinvoicing.SameTaxId);
            ServiceRequestWorkflow.WriteBilling(serviceRequest, request.Billing);
        }

        if (!string.IsNullOrWhiteSpace(request.AcceptorEmail))
            reissue.AcceptorEmail = request.AcceptorEmail.Trim();

        if (request.Reason is not null)
        {
            serviceRequest.InputValuesJson = ReinvoicingViews.InputJson(
                reissue.OriginalSiiNumber ?? reissue.OriginalSourceNumber, string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ReinvoicingDetailDto>.Success(
            await ReinvoicingViews.DetailAsync(dbContext, serviceRequest, reissue, clientView: true, cancellationToken));
    }
}

public sealed class SubmitReinvoicingCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    ReinvoicingService service,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<SubmitReinvoicingCommand, ReinvoicingDetailDto>
{
    public async Task<Result<ReinvoicingDetailDto>> Handle(SubmitReinvoicingCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ReinvoicingViews.LoadAsync(dbContext, accessEvaluator, request.Id, track: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ReinvoicingDetailDto>.Failure(loaded.Error);

        var (serviceRequest, reissue, scope) = loaded.Value;
        if (serviceRequest.OrganizationId != scope.OrganizationId || !scope.CanOperate)
            return Result<ReinvoicingDetailDto>.Failure(Error.Forbidden);

        var invoice = await dbContext.CustomerInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == reissue.OriginalInvoiceId, cancellationToken);
        if (invoice is null)
            return Result<ReinvoicingDetailDto>.Failure(DomainErrors.Invoice.NotFound(reissue.OriginalInvoiceId));

        var definition = await dbContext.ServiceDefinitions.AsNoTracking().FirstAsync(d => d.Id == serviceRequest.DefinitionId, cancellationToken);
        if (!definition.IsActive)
            return Result<ReinvoicingDetailDto>.Failure(DomainErrors.ServiceDefinition.NotFoundByCode(definition.Code));

        if (string.IsNullOrWhiteSpace(reissue.AcceptorEmail))
            reissue.AcceptorEmail = serviceRequest.BillingEmail?.Trim() ?? string.Empty;
        if (!ReinvoicingService.IsEmail(reissue.AcceptorEmail))
            return Result<ReinvoicingDetailDto>.Failure(DomainErrors.ServiceRequest.BillingDataRequired);

        var submitted = await service.SubmitAsync(
            serviceRequest, reissue, definition, invoice, ServiceActor.Client(currentUserService), request.AcceptTariff,
            request.AcceptedTotal, DateTime.UtcNow, cancellationToken);
        if (submitted.IsFailure)
            return Result<ReinvoicingDetailDto>.Failure(submitted.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        await workflow.NotifyAsync(serviceRequest, definition.NameEs, cancellationToken);

        return Result<ReinvoicingDetailDto>.Success(
            await ReinvoicingViews.DetailAsync(dbContext, serviceRequest, reissue, clientView: true, cancellationToken));
    }
}

public sealed class ResendReinvoicingAcceptanceCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ReinvoicingService service)
    : ICommandHandler<ResendReinvoicingAcceptanceCommand, ReinvoicingDetailDto>
{
    public async Task<Result<ReinvoicingDetailDto>> Handle(ResendReinvoicingAcceptanceCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ReinvoicingViews.LoadAsync(dbContext, accessEvaluator, request.Id, track: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ReinvoicingDetailDto>.Failure(loaded.Error);

        var (serviceRequest, reissue, scope) = loaded.Value;
        if (serviceRequest.OrganizationId != scope.OrganizationId || !scope.CanOperate)
            return Result<ReinvoicingDetailDto>.Failure(Error.Forbidden);
        if (reissue.AcceptanceStatus != ReinvoicingAcceptanceStatus.Pending || ServiceRequestStatus.Terminal.Contains(serviceRequest.Status))
            return Result<ReinvoicingDetailDto>.Failure(DomainErrors.Reinvoicing.AcceptanceNotPending);

        var invoice = await dbContext.CustomerInvoices.AsNoTracking().FirstAsync(i => i.Id == reissue.OriginalInvoiceId, cancellationToken);
        var now = DateTime.UtcNow;
        await service.RequestAcceptanceAsync(serviceRequest, reissue, invoice, now, cancellationToken);
        ServiceRequestWorkflow.AddEvent(dbContext, serviceRequest, serviceRequest.Status, serviceRequest.Status, ServiceActor.System,
            $"Enlace de aceptación reenviado a {reissue.AcceptorEmail}.", now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ReinvoicingDetailDto>.Success(
            await ReinvoicingViews.DetailAsync(dbContext, serviceRequest, reissue, clientView: true, cancellationToken));
    }
}

public sealed class GetReinvoicingQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetReinvoicingQuery, ReinvoicingDetailDto>
{
    public async Task<Result<ReinvoicingDetailDto>> Handle(GetReinvoicingQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ReinvoicingViews.LoadAsync(dbContext, accessEvaluator, request.Id, track: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<ReinvoicingDetailDto>.Failure(loaded.Error);

        var (serviceRequest, reissue, scope) = loaded.Value;
        var clientView = serviceRequest.OrganizationId == scope.OrganizationId && scope.CanOperate;
        return Result<ReinvoicingDetailDto>.Success(
            await ReinvoicingViews.DetailAsync(dbContext, serviceRequest, reissue, clientView, cancellationToken));
    }
}

/// <summary>Enlace de aceptación: búsqueda por el SHA-256 del token y vista para la nueva razón social.</summary>
internal static class ReinvoicingAcceptance
{
    public static async Task<(InvoiceReissue Reissue, ServiceRequest Request)?> FindAsync(
        IApplicationDbContext dbContext,
        string token,
        CancellationToken cancellationToken)
    {
        var hash = ReinvoicingService.Hash(token);
        var reissue = await dbContext.InvoiceReissues.FirstOrDefaultAsync(r => r.AcceptanceTokenHash == hash, cancellationToken);
        if (reissue is null)
            return null;

        var request = await dbContext.ServiceRequests.FirstOrDefaultAsync(r => r.Id == reissue.ServiceRequestId, cancellationToken);
        return request is null ? null : (reissue, request);
    }

    public static async Task<ReinvoicingAcceptanceViewDto> ViewAsync(
        IApplicationDbContext dbContext,
        InvoiceReissue reissue,
        ServiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await dbContext.CustomerInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == reissue.OriginalInvoiceId, cancellationToken);
        return new ReinvoicingAcceptanceViewDto(
            request.RequestNumber,
            reissue.OriginalSiiNumber ?? reissue.OriginalSourceNumber,
            reissue.OriginalLegalName,
            reissue.OriginalTaxId,
            invoice?.TotalAmount ?? 0m,
            invoice?.Currency ?? request.Currency ?? string.Empty,
            request.BillingName,
            request.BillingTaxId,
            reissue.FeeAmount + reissue.FeeTaxAmount,
            reissue.VatLossAmount,
            request.TotalAmount,
            request.Currency,
            reissue.AcceptanceStatus ?? ReinvoicingAcceptanceStatus.Pending,
            reissue.AcceptanceExpiresAt,
            reissue.AcceptedAt,
            reissue.DeclinedAt,
            BusinessCalendar.TimeZoneId(request.Country));
    }
}

public sealed class GetReinvoicingAcceptanceQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetReinvoicingAcceptanceQuery, ReinvoicingAcceptanceViewDto>
{
    public async Task<Result<ReinvoicingAcceptanceViewDto>> Handle(GetReinvoicingAcceptanceQuery request, CancellationToken cancellationToken)
    {
        var found = await ReinvoicingAcceptance.FindAsync(dbContext, request.Token, cancellationToken);
        if (found is null || found.Value.Request.Status == ServiceRequestStatus.Cancelled)
            return Result<ReinvoicingAcceptanceViewDto>.Failure(DomainErrors.Reinvoicing.AcceptanceNotFound);

        return Result<ReinvoicingAcceptanceViewDto>.Success(
            await ReinvoicingAcceptance.ViewAsync(dbContext, found.Value.Reissue, found.Value.Request, cancellationToken));
    }
}

public sealed class RespondReinvoicingAcceptanceCommandHandler(
    IApplicationDbContext dbContext,
    ReinvoicingService service)
    : ICommandHandler<RespondReinvoicingAcceptanceCommand, ReinvoicingAcceptanceViewDto>
{
    public async Task<Result<ReinvoicingAcceptanceViewDto>> Handle(RespondReinvoicingAcceptanceCommand request, CancellationToken cancellationToken)
    {
        var found = await ReinvoicingAcceptance.FindAsync(dbContext, request.Token, cancellationToken);
        if (found is null || found.Value.Request.Status == ServiceRequestStatus.Cancelled)
            return Result<ReinvoicingAcceptanceViewDto>.Failure(DomainErrors.Reinvoicing.AcceptanceNotFound);

        var (reissue, serviceRequest) = found.Value;
        var now = DateTime.UtcNow;
        if (reissue.AcceptanceStatus != ReinvoicingAcceptanceStatus.Pending)
            return Result<ReinvoicingAcceptanceViewDto>.Failure(DomainErrors.Reinvoicing.AcceptanceClosed);
        if (reissue.AcceptanceExpiresAt is { } expires && expires < now)
            return Result<ReinvoicingAcceptanceViewDto>.Failure(DomainErrors.Reinvoicing.AcceptanceExpired);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TaxId))
            return Result<ReinvoicingAcceptanceViewDto>.Failure(DomainErrors.Reinvoicing.AcceptorTaxIdRequired);

        var actor = new ServiceActor($"{request.Name.Trim()} ({TaxIdNormalizer.Normalize(request.TaxId)})", null, ServiceRequestActorKinds.Client);
        reissue.AcceptedByName = request.Name.Trim();
        reissue.AcceptedByTaxId = TaxIdNormalizer.Normalize(request.TaxId);
        reissue.AcceptedFromAddress = request.RemoteAddress;

        if (request.Accept)
        {
            reissue.AcceptanceStatus = ReinvoicingAcceptanceStatus.Accepted;
            reissue.AcceptedAt = now;
            ServiceRequestWorkflow.AddEvent(dbContext, serviceRequest, serviceRequest.Status, serviceRequest.Status, actor,
                $"Cobro aceptado por la nueva razón social {serviceRequest.BillingName} ({serviceRequest.BillingTaxId}).", now);

            // Ya pagada: la emisión va a la cola recuperable del pago (NF-03); si no, la encola la liberación del pago.
            if (serviceRequest.Status == ServiceRequestStatus.InProgress && serviceRequest.PaymentId is { } paymentId
                && !await dbContext.PaymentOutboxMessages.AnyAsync(
                    m => m.PaymentId == paymentId && m.JobType == PaymentOutboxJobTypes.Reinvoicing && m.Status == PaymentOutboxStatus.Pending,
                    cancellationToken))
            {
                PaymentLifecycle.Enqueue(dbContext, paymentId, PaymentOutboxJobTypes.Reinvoicing, now);
            }
        }
        else
        {
            reissue.AcceptanceStatus = ReinvoicingAcceptanceStatus.Declined;
            reissue.DeclinedAt = now;
            reissue.DeclineReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
            ServiceRequestWorkflow.AddEvent(dbContext, serviceRequest, serviceRequest.Status, serviceRequest.Status, actor,
                $"Cobro rechazado por la nueva razón social{(reissue.DeclineReason is null ? "." : $": {reissue.DeclineReason}")}", now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var accepted = reissue.AcceptanceStatus == ReinvoicingAcceptanceStatus.Accepted;
        await service.NotifyAsync(
            serviceRequest,
            accepted ? NotificationTypes.ReinvoicingAccepted : NotificationTypes.ReinvoicingDeclined,
            $"Refacturación {serviceRequest.RequestNumber} {(accepted ? "aceptada" : "rechazada")} por la nueva razón social",
            accepted
                ? $"{serviceRequest.BillingName} aceptó el cobro de la refacturación de la factura {reissue.OriginalSiiNumber ?? reissue.OriginalSourceNumber}. La nueva factura se emite con el pago."
                : $"{serviceRequest.BillingName} rechazó el cobro de la refacturación de la factura {reissue.OriginalSiiNumber ?? reissue.OriginalSourceNumber}. La factura no se emitirá; puede anular la solicitud antes del pago o contactar a Customer Service.",
            cancellationToken);

        return Result<ReinvoicingAcceptanceViewDto>.Success(
            await ReinvoicingAcceptance.ViewAsync(dbContext, reissue, serviceRequest, cancellationToken));
    }
}
