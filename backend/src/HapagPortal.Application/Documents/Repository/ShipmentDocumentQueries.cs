namespace HapagPortal.Application.Documents.Repository;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Invoices;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repositorio documental del embarque (M6-09): en una sola vista los documentos emitidos del BL que el
/// usuario puede ver según M1-11 (el comprobante Collect solo para la agencia de aduanas autorizada, M6-04;
/// la copia valorada solo para quien puede solicitarla, M6-05), los recibos de sus pagos y las facturas
/// del BL, y qué documentos puede solicitar.
/// </summary>
public sealed record GetShipmentDocumentsQuery(string BlNumber) : IQuery<ShipmentDocumentsDto>;

/// <summary>
/// Descarga de un documento con registro de usuario, organización, mandante y fecha (NF-14). El asistente
/// (M10-04) usa el mismo comando con el canal <c>Assistant</c>: aplica las mismas restricciones.
/// </summary>
public sealed record DownloadShipmentDocumentCommand(
    string BlNumber,
    Guid DocumentId,
    string Channel = DocumentChannels.Portal) : ICommand<DocumentFileDto>;

/// <summary>Reenvía el documento al correo registrado de la organización del usuario (M6-05) y lo registra.</summary>
public sealed record SendShipmentDocumentCommand(string BlNumber, Guid DocumentId) : ICommand<DocumentDeliveryDto>;

public sealed class GetShipmentDocumentsQueryValidator : AbstractValidator<GetShipmentDocumentsQuery>
{
    public GetShipmentDocumentsQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class DownloadShipmentDocumentCommandValidator : AbstractValidator<DownloadShipmentDocumentCommand>
{
    public DownloadShipmentDocumentCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Channel)
            .Must(c => c is DocumentChannels.Portal or DocumentChannels.Assistant)
            .WithMessage("Channel must be Portal or Assistant.");
    }
}

public sealed class SendShipmentDocumentCommandValidator : AbstractValidator<SendShipmentDocumentCommand>
{
    public SendShipmentDocumentCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}

/// <summary>Actor de una operación documental (NF-14): usuario, su organización y el mandante si actúa bajo un acceso.</summary>
public static class DocumentActors
{
    public static DocumentActor From(ICurrentUserService currentUser, AccessScope scope, ShipmentGrantAccess? grant) =>
        new(currentUser.UserId, currentUser.Email, scope.OrganizationId, grant?.GrantorOrganizationId, grant?.GrantId);
}

public sealed class GetShipmentDocumentsQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IChargeRulesService chargeRulesService,
    IResponsibilityLetterStatus responsibilityLetterStatus)
    : IQueryHandler<GetShipmentDocumentsQuery, ShipmentDocumentsDto>
{
    private const string ReceiptKind = "Receipt";
    private const string InvoiceKind = "Invoice";

    public async Task<Result<ShipmentDocumentsDto>> Handle(GetShipmentDocumentsQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<ShipmentDocumentsDto>.Failure(loaded.Error);

        var (bl, permissions, payer, scope) = loaded.Value;

        var documents = (await dbContext.ShipmentDocuments.AsNoTracking()
                .Where(d => d.BillOfLadingId == bl.Id)
                .OrderByDescending(d => d.IssuedAt)
                .ToListAsync(cancellationToken))
            .Where(d => ShipmentDocumentService.CanView(permissions, d.DocumentType))
            .ToList();

        var organizationIds = documents.Where(d => d.IssuedForOrganizationId is not null).Select(d => d.IssuedForOrganizationId!.Value).Distinct().ToList();
        var organizations = await dbContext.Clients.AsNoTracking()
            .Where(c => organizationIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var related = await RelatedAsync(bl.Id, scope, cancellationToken);

        // Las solicitudes son de la propia organización: la visibilidad total del administrador no las habilita.
        bool CanRequest(string action) => !scope.IsAdmin && permissions.CanExecute(action);

        var actions = new DocumentActionsDto(
            CanRequest(ShipmentActionCodes.RequestValuedBlCopy),
            CanRequest(ShipmentActionCodes.RequestUnvaluedBlCopy),
            CanRequest(ShipmentActionCodes.GenerateResponsibilityLetter),
            NoDebtEvaluator.IsApplicable(bl) && CanRequest(ShipmentActionCodes.DownloadNoDebtCertificate),
            bl.Country == CountryCodes.Chile && CanRequest(ShipmentActionCodes.GenerateTransshipmentCertificate));

        ResponsibilityLetterStateDto? letter = null;
        var conditions = await chargeRulesService.GetConditionsAsync(payer, cancellationToken);
        if (conditions.ResponsibilityLetterRequired || actions.CanIssueResponsibilityLetter)
        {
            var status = await responsibilityLetterStatus.GetStatusAsync(bl.Id, payer.Id, cancellationToken);
            letter = new ResponsibilityLetterStateDto(
                conditions.ResponsibilityLetterRequired,
                status,
                BlocksProcess: conditions.ResponsibilityLetterRequired && status != ProcessRequirementStatus.Fulfilled);
        }

        return Result<ShipmentDocumentsDto>.Success(new ShipmentDocumentsDto(
            bl.Id,
            bl.BLNumber,
            bl.BookingNumber,
            bl.Country,
            BusinessCalendar.TimeZoneId(bl.Country),
            documents.Select(d => ShipmentDocumentService.ToDto(
                d, d.IssuedForOrganizationId is { } id ? organizations.GetValueOrDefault(id) : null)).ToList(),
            related,
            actions,
            letter));
    }

    /// <summary>Recibos de los pagos de la organización (como pagadora o mandante) que incluyen el BL y facturas visibles del BL.</summary>
    private async Task<IReadOnlyList<RelatedDocumentDto>> RelatedAsync(Guid blId, AccessScope scope, CancellationToken cancellationToken)
    {
        var paymentIds = dbContext.PaymentDetails.Where(d => d.BillOfLadingId == blId).Select(d => d.PaymentId);
        var payments = dbContext.Payments.AsNoTracking()
            .Where(p => (p.BillOfLadingId == blId || paymentIds.Contains(p.Id))
                && (p.ReceiptNumber != null || p.SlipNumber != null)
                && p.Status != PaymentStatus.Cancelled);

        if (!scope.IsAdmin)
        {
            var own = scope.OrganizationId;
            payments = payments.Where(p => p.ClientId == own || p.OnBehalfOfClientId == own);
        }

        var receipts = (await payments.ToListAsync(cancellationToken))
            .Select(p => new RelatedDocumentDto(
                ReceiptKind,
                p.Id,
                p.ReceiptNumber ?? p.SlipNumber!,
                p.ConfirmedAt ?? p.SlipIssuedAt ?? p.PaymentDate,
                p.PaymentNumber,
                $"/api/v1/payment-history/{p.Id}/receipt"));

        var invoiceScope = await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        var invoices = (await dbContext.CustomerInvoices.AsNoTracking()
                .Where(i => i.BillOfLadingId == blId && i.SiiNumber != null)
                .ToListAsync(cancellationToken))
            .Where(i => InvoiceAccess.CanView(invoiceScope, i))
            .Select(i => new RelatedDocumentDto(
                InvoiceKind,
                i.Id,
                i.SiiNumber!,
                i.IssueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                i.SourceNumber,
                $"/api/v1/invoices/{i.Id}/pdf"));

        return receipts.Concat(invoices).OrderByDescending(r => r.IssuedAt).ToList();
    }
}

public sealed class DownloadShipmentDocumentCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    ShipmentDocumentService documents)
    : ICommandHandler<DownloadShipmentDocumentCommand, DocumentFileDto>
{
    public async Task<Result<DocumentFileDto>> Handle(DownloadShipmentDocumentCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<DocumentFileDto>.Failure(loaded.Error);

        var (bl, permissions, _, scope) = loaded.Value;

        // NF-05: un documento de otro BL o de un tipo no permitido no se distingue de uno inexistente.
        var document = await dbContext.ShipmentDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.BillOfLadingId == bl.Id, cancellationToken);
        if (document is null || !ShipmentDocumentService.CanView(permissions, document.DocumentType))
            return Result<DocumentFileDto>.Failure(DomainErrors.ShipmentDocument.NotFound(request.DocumentId));

        var file = await documents.ReadAsync(document, cancellationToken);
        if (file.IsFailure)
            return file;

        var grant = permissions.GrantFor(ShipmentDocumentAccess.ActionFor(document.DocumentType));
        documents.Log(
            document,
            ShipmentDocumentEventTypes.Downloaded,
            request.Channel,
            DocumentActors.From(currentUserService, scope, grant),
            DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return file;
    }
}

public sealed class SendShipmentDocumentCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    ShipmentDocumentService documents)
    : ICommandHandler<SendShipmentDocumentCommand, DocumentDeliveryDto>
{
    public async Task<Result<DocumentDeliveryDto>> Handle(SendShipmentDocumentCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<DocumentDeliveryDto>.Failure(loaded.Error);

        var (bl, permissions, organization, scope) = loaded.Value;

        var document = await dbContext.ShipmentDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.BillOfLadingId == bl.Id, cancellationToken);
        if (document is null || !ShipmentDocumentService.CanView(permissions, document.DocumentType))
            return Result<DocumentDeliveryDto>.Failure(DomainErrors.ShipmentDocument.NotFound(request.DocumentId));

        var grant = permissions.GrantFor(ShipmentDocumentAccess.ActionFor(document.DocumentType));
        var sent = await documents.SendAsync(
            document,
            [organization.Email],
            DocumentChannels.Email,
            DocumentActors.From(currentUserService, scope, grant),
            DateTime.UtcNow,
            cancellationToken);
        if (sent.IsFailure)
            return Result<DocumentDeliveryDto>.Failure(sent.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<DocumentDeliveryDto>.Success(new DocumentDeliveryDto(
            ShipmentDocumentService.ToDto(document, document.IssuedForOrganizationId == organization.Id ? organization.Name : null),
            sent.Value));
    }
}
