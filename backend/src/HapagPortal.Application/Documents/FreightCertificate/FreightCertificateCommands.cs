namespace HapagPortal.Application.Documents.FreightCertificate;

using System.Text.Json.Nodes;
using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Flete del BL que certifica el documento (M6-02).</summary>
public sealed record FreightInfoDto(decimal Amount, string? Currency, string? Terms);

/// <summary>Consignatario del BL con que se precarga la solicitud.</summary>
public sealed record FreightCertificateConsigneeDto(string? Name, string? TaxId);

/// <summary>
/// Gestión del certificado de flete del BL (M6-02): si aplica (importación de Bolivia), si el usuario puede solicitarlo
/// (M1-11 y perfil que opera), el modo de cobro (<c>Free</c>: sin pago ni carro en esta entrega), el flete que se
/// certifica, el consignatario del BL, las finalidades admitidas, las solicitudes de la organización y los certificados
/// emitidos que puede ver.
/// </summary>
public sealed record FreightCertificateContextDto(
    Guid BlId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    bool Applicable,
    bool CanRequest,
    string PaymentMode,
    bool RequiresPayment,
    FreightInfoDto Freight,
    FreightCertificateConsigneeDto Consignee,
    IReadOnlyList<string> Purposes,
    IReadOnlyList<ServiceRequestSummaryDto> Requests,
    IReadOnlyList<ShipmentDocumentDto> Documents);

/// <summary>Solicitud registrada (completada) con el certificado emitido y los correos a los que se envió.</summary>
public sealed record FreightCertificateResultDto(
    ServiceRequestDetailDto Request,
    ShipmentDocumentDto Document,
    IReadOnlyList<string> SentTo);

public sealed record GetFreightCertificateQuery(string BlNumber) : IQuery<FreightCertificateContextDto>;

/// <summary>
/// Solicitud del certificado de flete (M6-02, BO-IMP-08), importación de Bolivia: el cliente ingresa los datos del
/// consignatario y la finalidad; el portal registra la solicitud (número, estados y línea de tiempo del modelo de
/// servicios), genera el PDF firmado con la plantilla (IDocumentSigner), lo publica en el repositorio (M6-09) y lo envía
/// al correo registrado de la organización. Primera entrega de Fase 2: sin pago ni carro (decisión Q1 de la v4); el modo
/// pagado en BOB queda reservado (<c>Documents:FreightCertificateMode</c>).
/// </summary>
public sealed record RequestFreightCertificateCommand(
    string BlNumber,
    string ConsigneeName,
    string ConsigneeTaxId,
    string Purpose,
    string? Recipient,
    string? Notes,
    bool SendEmail = true) : ICommand<FreightCertificateResultDto>;

public sealed class GetFreightCertificateQueryValidator : AbstractValidator<GetFreightCertificateQuery>
{
    public GetFreightCertificateQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestFreightCertificateCommandValidator : AbstractValidator<RequestFreightCertificateCommand>
{
    public RequestFreightCertificateCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ConsigneeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConsigneeTaxId).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Purpose)
            .Must(p => FreightCertificatePurposes.All.Contains(p))
            .WithMessage($"Purpose must be one of: {string.Join(", ", FreightCertificatePurposes.All)}.");
        RuleFor(x => x.Recipient).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class GetFreightCertificateQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    DocumentSettings settings)
    : IQueryHandler<GetFreightCertificateQuery, FreightCertificateContextDto>
{
    public async Task<Result<FreightCertificateContextDto>> Handle(GetFreightCertificateQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ServiceShipmentLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, null, forOperation: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<FreightCertificateContextDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var (bl, permissions, organization, scope) = context;
        if (!permissions.Can(ShipmentActionCodes.GenerateFreightCertificate))
            return Result<FreightCertificateContextDto>.Failure(Error.Forbidden);

        var applicable = DocumentServiceRequests.IsBoliviaImport(bl);
        var definition = await DocumentServiceRequests.DefinitionAsync(
            dbContext, ServiceDefinitionCodes.FreightCertificate, context, cancellationToken);

        var consignee = await dbContext.BLParties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.BillOfLadingId == bl.Id && p.Role == ShipmentRoleCodes.Consignee, cancellationToken);

        var view = new ShipmentServiceContextView(bl.Id, permissions, organization.Id, organization.Name);

        return Result<FreightCertificateContextDto>.Success(new FreightCertificateContextDto(
            bl.Id,
            bl.BLNumber,
            bl.BookingNumber,
            bl.Country,
            applicable,
            CanRequest: applicable && definition.IsSuccess && !scope.IsAdmin
                && permissions.CanExecute(ShipmentActionCodes.GenerateFreightCertificate),
            settings.FreightCertificateMode,
            RequiresPayment: false,
            new FreightInfoDto(bl.FreightAmount, bl.FreightCurrency, bl.FreightTerms),
            new FreightCertificateConsigneeDto(
                consignee?.Name ?? bl.Consignee,
                consignee?.TaxId is null ? null : TaxIdNormalizer.Normalize(consignee.TaxId)),
            FreightCertificatePurposes.All,
            scope.IsAdmin
                ? []
                : await DocumentServiceRequests.OwnRequestsAsync(
                    dbContext, ServiceDefinitionCodes.FreightCertificate, bl.Id, organization.Id, cancellationToken),
            await DocumentServiceRequests.VisibleDocumentsAsync(dbContext, view, ShipmentDocumentTypes.FreightCertificate, cancellationToken)));
    }
}

public sealed class RequestFreightCertificateCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    ServiceRequestWorkflow workflow,
    ShipmentDocumentService documents)
    : ICommandHandler<RequestFreightCertificateCommand, FreightCertificateResultDto>
{
    public async Task<Result<FreightCertificateResultDto>> Handle(RequestFreightCertificateCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ServiceShipmentLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, null, forOperation: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<FreightCertificateResultDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var (bl, permissions, organization, scope) = context;

        if (!DocumentServiceRequests.IsBoliviaImport(bl))
            return Result<FreightCertificateResultDto>.Failure(DomainErrors.FreightCertificate.NotApplicable);

        // M1-11: consignee (O); tercero y transportista solo con un acceso otorgado que lo incluya (X(o)).
        if (!permissions.CanExecute(ShipmentActionCodes.GenerateFreightCertificate))
            return Result<FreightCertificateResultDto>.Failure(Error.Forbidden);

        var definitionResult = await DocumentServiceRequests.DefinitionAsync(
            dbContext, ServiceDefinitionCodes.FreightCertificate, context, cancellationToken);
        if (definitionResult.IsFailure)
            return Result<FreightCertificateResultDto>.Failure(definitionResult.Error);

        var definition = definitionResult.Value;
        var values = new JsonObject
        {
            [FreightCertificateFields.ConsigneeName] = request.ConsigneeName.Trim(),
            [FreightCertificateFields.ConsigneeTaxId] = request.ConsigneeTaxId.Trim(),
            [FreightCertificateFields.Purpose] = request.Purpose
        };
        if (!string.IsNullOrWhiteSpace(request.Recipient))
            values[FreightCertificateFields.Recipient] = request.Recipient.Trim();
        if (!string.IsNullOrWhiteSpace(request.Notes))
            values[FreightCertificateFields.Notes] = request.Notes.Trim();

        var input = await workflow.ValidateInputAsync(
            definition, bl, null, values.ToJsonString(), requireComplete: true, cancellationToken);
        if (input.IsFailure)
            return ServiceRequestViews.Fail<FreightCertificateResultDto>(input);

        var now = DateTime.UtcNow;
        var actor = ServiceActor.Client(currentUserService);
        var created = DocumentServiceRequests.CreateSubmitted(
            dbContext, definition, context, currentUserService, actor, input.Value.NormalizedJson, [], now);
        if (created.IsFailure)
            return Result<FreightCertificateResultDto>.Failure(created.Error);

        var serviceRequest = created.Value;
        var documentActor = DocumentActors.From(currentUserService, scope, permissions.GrantFor(ShipmentActionCodes.GenerateFreightCertificate));
        var data = await documents.LoadDataAsync(bl, cancellationToken);
        var header = documents.NewHeader(ShipmentDocumentTypes.FreightCertificate, bl, now);
        var model = ShipmentDocumentTemplates.FreightCertificate(data, header, new FreightCertificateData(
            serviceRequest.RequestNumber,
            organization.Name,
            TaxIdNormalizer.Normalize(organization.TaxId),
            request.ConsigneeName.Trim(),
            request.ConsigneeTaxId.Trim(),
            PurposeLabel(request.Purpose),
            request.Recipient?.Trim(),
            request.Notes?.Trim()));

        var issued = await documents.IssueAsync(
            new DocumentIssue(
                ShipmentDocumentTypes.FreightCertificate,
                bl,
                model,
                ShipmentDocumentOrigins.Request,
                documentActor,
                organization.Id,
                data.Containers.Select(c => c.ContainerNumber).ToList(),
                GenerationKey: DocumentServiceRequests.GenerationKey(serviceRequest.Id),
                RecipientEmails: request.SendEmail ? [organization.Email] : null),
            cancellationToken);
        if (issued.IsFailure)
            return Result<FreightCertificateResultDto>.Failure(issued.Error);

        // Sin cobro en esta entrega: la solicitud se completa con la emisión del certificado.
        var completed = ServiceRequestWorkflow.Fulfill(dbContext, serviceRequest, definition, ServiceActor.System,
            $"Certificado {issued.Value.DocumentNumber} emitido, sin cobro (primera entrega de Fase 2, M6-02).", now);
        if (completed.IsFailure)
            return Result<FreightCertificateResultDto>.Failure(completed.Error);
        serviceRequest.ResolutionNotes = $"Certificado {issued.Value.DocumentNumber} emitido.";

        IReadOnlyList<string> sentTo = [];
        if (request.SendEmail)
        {
            var sent = await documents.SendAsync(issued.Value, [organization.Email], DocumentChannels.Email, documentActor, now, cancellationToken);
            if (sent.IsFailure)
                return Result<FreightCertificateResultDto>.Failure(sent.Error);
            sentTo = sent.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await workflow.NotifyAsync(serviceRequest, definition.NameEs, cancellationToken);

        return Result<FreightCertificateResultDto>.Success(new FreightCertificateResultDto(
            await ServiceRequestViews.DetailAsync(dbContext, serviceRequest, clientView: true, cancellationToken),
            ShipmentDocumentService.ToDto(issued.Value, organization.Name),
            sentTo));
    }

    private static string PurposeLabel(string purpose) => purpose switch
    {
        FreightCertificatePurposes.Customs => "Trámite aduanero",
        FreightCertificatePurposes.Insurance => "Seguro de la carga",
        FreightCertificatePurposes.Bank => "Trámite bancario",
        _ => "Otra"
    };
}
