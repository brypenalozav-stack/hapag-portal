namespace HapagPortal.Application.Documents.ReleaseLetter;

using System.Text.Json.Nodes;
using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Unidad del BL que puede incluirse en la carta, con su TATC y la solicitud pendiente que ya la incluye.</summary>
public sealed record ReleaseLetterContainerOptionDto(
    string ContainerNumber,
    string ContainerType,
    string Status,
    ReleaseLetterTatcContainerDto? Tatc,
    string? PendingRequestNumber);

/// <summary>Transportista registrado que la organización vinculó al BL (acceso otorgado) o pre-creó (M1-09).</summary>
public sealed record ReleaseLetterCarrierOptionDto(Guid OrganizationId, string Name, string TaxId, string Source);

/// <summary>Consignatario del BL con que se precarga la solicitud.</summary>
public sealed record ReleaseLetterConsigneeDto(string? Name, string? TaxId);

/// <summary>
/// Gestión de la carta de liberación y desconsolidado del BL (M6-08): si aplica (importación de Bolivia), si el usuario
/// puede solicitarla (M1-11 y perfil que opera), las unidades con su TATC, los transportistas registrados elegibles, los
/// tipos de sociedad, la regla de TATC vigente, las solicitudes de la organización y las cartas que puede ver.
/// </summary>
public sealed record ReleaseLetterContextDto(
    Guid BlId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    bool Applicable,
    bool CanRequest,
    bool RequiresIssuedTatc,
    IReadOnlyList<string> LegalEntityTypes,
    ReleaseLetterConsigneeDto Consignee,
    IReadOnlyList<ReleaseLetterContainerOptionDto> Containers,
    ReleaseLetterTatcDto Tatc,
    IReadOnlyList<ReleaseLetterCarrierOptionDto> Carriers,
    IReadOnlyList<ServiceRequestSummaryDto> Requests,
    IReadOnlyList<ShipmentDocumentDto> Documents);

public sealed record GetReleaseLetterQuery(string BlNumber) : IQuery<ReleaseLetterContextDto>;

/// <summary>
/// Solicitud de la carta de liberación y desconsolidado (M6-08, BO-IMP-11): BL de importación de Bolivia, unidades
/// seleccionadas, consignatario según el tipo de sociedad y transportista (registrado o datos libres). Registra el TATC
/// de las unidades y deriva la solicitud a Customer Service para su aprobación; la carta se emite al aprobarse.
/// </summary>
public sealed record RequestReleaseLetterCommand(
    string BlNumber,
    IReadOnlyList<string> Containers,
    string LegalEntityType,
    string ConsigneeName,
    string ConsigneeTaxId,
    string? ConsigneeAddress,
    string? LegalRepresentativeName,
    string? LegalRepresentativeId,
    Guid? CarrierOrganizationId,
    string? CarrierName,
    string? CarrierTaxId,
    string? DriverName,
    string? DriverId,
    string? TruckPlate,
    string? Observations) : ICommand<ReleaseLetterRequestDto>;

/// <summary>Carta de la organización del usuario (como solicitante o mandante).</summary>
public sealed record GetReleaseLetterRequestQuery(Guid Id) : IQuery<ReleaseLetterRequestDto>;

/// <summary>Vista interna para Customer Service: agrega el TATC consultado ahora y el registro de Counter (M8-09).</summary>
public sealed record GetInternalReleaseLetterRequestQuery(Guid Id) : IQuery<ReleaseLetterRequestDto>;

public sealed class GetReleaseLetterQueryValidator : AbstractValidator<GetReleaseLetterQuery>
{
    public GetReleaseLetterQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestReleaseLetterCommandValidator : AbstractValidator<RequestReleaseLetterCommand>
{
    public RequestReleaseLetterCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Containers).NotEmpty().WithMessage("Select at least one container of the shipment.");
        RuleForEach(x => x.Containers).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LegalEntityType)
            .Must(t => LegalEntityTypes.All.Contains(t))
            .WithMessage("LegalEntityType must be COMPANY or NATURAL_PERSON.");
        RuleFor(x => x.ConsigneeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConsigneeTaxId).NotEmpty().MaximumLength(30);
        RuleFor(x => x.ConsigneeAddress).MaximumLength(300);
        RuleFor(x => x.LegalRepresentativeName).MaximumLength(200);
        RuleFor(x => x.LegalRepresentativeId).MaximumLength(30);

        // Empresa: domicilio y representante legal con su documento (definición provisoria, M6-08).
        When(x => x.LegalEntityType == LegalEntityTypes.Company, () =>
        {
            RuleFor(x => x.ConsigneeAddress).NotEmpty().WithMessage("The address is required for a company.");
            RuleFor(x => x.LegalRepresentativeName).NotEmpty().WithMessage("The legal representative is required for a company.");
            RuleFor(x => x.LegalRepresentativeId).NotEmpty().WithMessage("The legal representative's ID is required for a company.");
        });

        RuleFor(x => x.CarrierName).MaximumLength(200);
        RuleFor(x => x.CarrierTaxId).MaximumLength(30);
        RuleFor(x => x.DriverName).MaximumLength(200);
        RuleFor(x => x.DriverId).MaximumLength(30);
        RuleFor(x => x.TruckPlate).MaximumLength(20);
        RuleFor(x => x.Observations).MaximumLength(1000);
    }
}

/// <summary>Transportistas registrados elegibles para la carta de una organización sobre un BL.</summary>
internal static class ReleaseLetterCarriers
{
    public const string GrantSource = "Grant";
    public const string PreCreatedSource = "PreCreated";

    public static async Task<IReadOnlyList<ReleaseLetterCarrierOptionDto>> ForAsync(
        IApplicationDbContext dbContext,
        Guid billOfLadingId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var granted = await dbContext.AccessGrants.AsNoTracking()
            .Where(g => g.GrantorClientId == organizationId
                && g.BillOfLadingId == billOfLadingId
                && (g.Status == AccessGrantStatus.Active || g.Status == AccessGrantStatus.PendingActivation))
            .Select(g => g.GranteeClientId)
            .ToListAsync(cancellationToken);
        var preCreated = await dbContext.CarrierPreRegistrations.AsNoTracking()
            .Where(p => p.RequestedByOrganizationId == organizationId)
            .Select(p => p.CarrierOrganizationId)
            .ToListAsync(cancellationToken);

        var ids = granted.Concat(preCreated).Distinct().ToList();
        var carriers = await dbContext.Clients.AsNoTracking()
            .Where(c => ids.Contains(c.Id) && c.OrganizationType == OrganizationTypes.Carrier && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return carriers
            .Select(c => new ReleaseLetterCarrierOptionDto(
                c.Id, c.Name, TaxIdNormalizer.Normalize(c.TaxId), granted.Contains(c.Id) ? GrantSource : PreCreatedSource))
            .ToList();
    }
}

public sealed class GetReleaseLetterQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ReleaseLetterService releaseLetters)
    : IQueryHandler<GetReleaseLetterQuery, ReleaseLetterContextDto>
{
    public async Task<Result<ReleaseLetterContextDto>> Handle(GetReleaseLetterQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ServiceShipmentLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, null, forOperation: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<ReleaseLetterContextDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var (bl, permissions, organization, scope) = context;
        if (!permissions.Can(ShipmentActionCodes.GenerateReleaseLetter))
            return Result<ReleaseLetterContextDto>.Failure(Error.Forbidden);

        var applicable = DocumentServiceRequests.IsBoliviaImport(bl);
        var definition = await DocumentServiceRequests.DefinitionAsync(dbContext, ServiceDefinitionCodes.ReleaseLetter, context, cancellationToken);
        var now = DateTime.UtcNow;

        var containers = await dbContext.BLContainers.AsNoTracking()
            .Where(c => c.BillOfLadingId == bl.Id)
            .OrderBy(c => c.ContainerNumber)
            .ToListAsync(cancellationToken);
        var tatc = applicable
            ? await releaseLetters.CheckTatcAsync(bl.BLNumber, containers.Select(c => c.ContainerNumber).ToList(), now, cancellationToken)
            : new ReleaseLetterTatcDto(false, null, null, now, []);

        var pending = scope.IsAdmin
            ? []
            : await dbContext.ServiceRequests.AsNoTracking()
                .Where(r => r.DefinitionCode == ServiceDefinitionCodes.ReleaseLetter
                    && r.BillOfLadingId == bl.Id
                    && r.OrganizationId == organization.Id
                    && r.Status == ServiceRequestStatus.PendingApproval)
                .ToListAsync(cancellationToken);

        var options = containers.Select(c => new ReleaseLetterContainerOptionDto(
                c.ContainerNumber,
                c.ContainerType,
                c.Status,
                tatc.Containers.FirstOrDefault(t => string.Equals(t.ContainerNumber, c.ContainerNumber, StringComparison.OrdinalIgnoreCase)),
                pending.FirstOrDefault(r => ServiceRequestViews.Containers(r).Contains(c.ContainerNumber, StringComparer.OrdinalIgnoreCase))?.RequestNumber))
            .ToList();

        var consignee = await dbContext.BLParties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.BillOfLadingId == bl.Id && p.Role == ShipmentRoleCodes.Consignee, cancellationToken);
        var view = new ShipmentServiceContextView(bl.Id, permissions, organization.Id, organization.Name);

        return Result<ReleaseLetterContextDto>.Success(new ReleaseLetterContextDto(
            bl.Id,
            bl.BLNumber,
            bl.BookingNumber,
            bl.Country,
            applicable,
            CanRequest: applicable && definition.IsSuccess && !scope.IsAdmin
                && permissions.CanExecute(ShipmentActionCodes.GenerateReleaseLetter) && containers.Count > 0,
            releaseLetters.RequiresIssuedTatc,
            LegalEntityTypes.All,
            new ReleaseLetterConsigneeDto(
                consignee?.Name ?? bl.Consignee,
                consignee?.TaxId is null ? null : TaxIdNormalizer.Normalize(consignee.TaxId)),
            options,
            tatc,
            scope.IsAdmin ? [] : await ReleaseLetterCarriers.ForAsync(dbContext, bl.Id, organization.Id, cancellationToken),
            scope.IsAdmin
                ? []
                : await DocumentServiceRequests.OwnRequestsAsync(dbContext, ServiceDefinitionCodes.ReleaseLetter, bl.Id, organization.Id, cancellationToken),
            await DocumentServiceRequests.VisibleDocumentsAsync(dbContext, view, ShipmentDocumentTypes.ReleaseLetter, cancellationToken)));
    }
}

public sealed class RequestReleaseLetterCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    ServiceRequestWorkflow workflow,
    ReleaseLetterService releaseLetters)
    : ICommandHandler<RequestReleaseLetterCommand, ReleaseLetterRequestDto>
{
    public async Task<Result<ReleaseLetterRequestDto>> Handle(RequestReleaseLetterCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ServiceShipmentLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, null, forOperation: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ReleaseLetterRequestDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var (bl, permissions, organization, _) = context;

        if (!DocumentServiceRequests.IsBoliviaImport(bl))
            return Result<ReleaseLetterRequestDto>.Failure(DomainErrors.ReleaseLetter.NotApplicable);

        // M1-11: consignee (O); tercero solo con un acceso otorgado que lo incluya (X(o)); el resto, nunca.
        if (!permissions.CanExecute(ShipmentActionCodes.GenerateReleaseLetter))
            return Result<ReleaseLetterRequestDto>.Failure(Error.Forbidden);

        var definitionResult = await DocumentServiceRequests.DefinitionAsync(dbContext, ServiceDefinitionCodes.ReleaseLetter, context, cancellationToken);
        if (definitionResult.IsFailure)
            return Result<ReleaseLetterRequestDto>.Failure(definitionResult.Error);
        var definition = definitionResult.Value;

        // Transportista: registrado (vinculado al BL por la organización o pre-creado por ella) o datos libres.
        string carrierName;
        string? carrierTaxId;
        if (request.CarrierOrganizationId is { } carrierId)
        {
            var carriers = await ReleaseLetterCarriers.ForAsync(dbContext, bl.Id, organization.Id, cancellationToken);
            var carrier = carriers.FirstOrDefault(c => c.OrganizationId == carrierId);
            if (carrier is null)
                return Result<ReleaseLetterRequestDto>.Failure(DomainErrors.ReleaseLetter.CarrierNotFound);
            carrierName = carrier.Name;
            carrierTaxId = carrier.TaxId;
        }
        else if (string.IsNullOrWhiteSpace(request.CarrierName) || string.IsNullOrWhiteSpace(request.CarrierTaxId))
        {
            return Result<ReleaseLetterRequestDto>.Failure(DomainErrors.ReleaseLetter.CarrierRequired);
        }
        else
        {
            carrierName = request.CarrierName.Trim();
            carrierTaxId = request.CarrierTaxId.Trim();
        }

        var company = request.LegalEntityType == LegalEntityTypes.Company;
        var values = new JsonObject
        {
            [ReleaseLetterFields.Containers] = new JsonArray(request.Containers.Select(c => (JsonNode?)JsonValue.Create(c.Trim())).ToArray()),
            [ReleaseLetterFields.LegalEntityType] = request.LegalEntityType,
            [ReleaseLetterFields.ConsigneeName] = request.ConsigneeName.Trim(),
            [ReleaseLetterFields.ConsigneeTaxId] = request.ConsigneeTaxId.Trim(),
            [ReleaseLetterFields.CarrierName] = carrierName,
            [ReleaseLetterFields.CarrierTaxId] = carrierTaxId
        };
        Add(values, ReleaseLetterFields.ConsigneeAddress, request.ConsigneeAddress);
        if (company)
        {
            Add(values, ReleaseLetterFields.LegalRepresentativeName, request.LegalRepresentativeName);
            Add(values, ReleaseLetterFields.LegalRepresentativeId, request.LegalRepresentativeId);
        }
        Add(values, ReleaseLetterFields.DriverName, request.DriverName);
        Add(values, ReleaseLetterFields.DriverId, request.DriverId);
        Add(values, ReleaseLetterFields.TruckPlate, request.TruckPlate);
        Add(values, ReleaseLetterFields.Observations, request.Observations);

        var input = await workflow.ValidateInputAsync(definition, bl, null, values.ToJsonString(), requireComplete: true, cancellationToken);
        if (input.IsFailure)
            return ServiceRequestViews.Fail<ReleaseLetterRequestDto>(input);

        // Una unidad no puede estar en dos cartas pendientes de la misma organización.
        var selected = input.Value.Containers;
        var pending = await dbContext.ServiceRequests.AsNoTracking()
            .Where(r => r.DefinitionCode == ServiceDefinitionCodes.ReleaseLetter
                && r.BillOfLadingId == bl.Id
                && r.OrganizationId == organization.Id
                && r.Status == ServiceRequestStatus.PendingApproval)
            .ToListAsync(cancellationToken);
        if (pending.Any(r => ServiceRequestViews.Containers(r).Intersect(selected, StringComparer.OrdinalIgnoreCase).Any()))
            return Result<ReleaseLetterRequestDto>.Failure(DomainErrors.ReleaseLetter.AlreadyRequested);

        var now = DateTime.UtcNow;
        var tatc = await releaseLetters.CheckTatcAsync(bl.BLNumber, selected, now, cancellationToken);

        var actor = ServiceActor.Client(currentUserService);
        var created = DocumentServiceRequests.CreateSubmitted(
            dbContext, definition, context, currentUserService, actor, input.Value.NormalizedJson, selected, now);
        if (created.IsFailure)
            return Result<ReleaseLetterRequestDto>.Failure(created.Error);

        var serviceRequest = created.Value;
        var letter = new ReleaseLetterRequest
        {
            ServiceRequestId = serviceRequest.Id,
            LegalEntityType = request.LegalEntityType,
            CarrierOrganizationId = request.CarrierOrganizationId
        };
        ReleaseLetterService.RecordSubmission(letter, tatc);
        dbContext.ReleaseLetterRequests.Add(letter);

        // Circuito de aprobación: el equipo de la definición (Customer Service) revisa la solicitud con el TATC registrado.
        serviceRequest.AssignedTeam = definition.ApprovalTeam == ServiceTeams.None ? ServiceTeams.CustomerService : definition.ApprovalTeam;
        var derived = ServiceRequestWorkflow.Transition(dbContext, serviceRequest, ServiceRequestStatus.PendingApproval, ServiceActor.System,
            $"Derivada al equipo {serviceRequest.AssignedTeam}. Al enviar: {ReleaseLetterService.Describe(tatc)}", now);
        if (derived.IsFailure)
            return Result<ReleaseLetterRequestDto>.Failure(derived.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        await workflow.NotifyAsync(serviceRequest, definition.NameEs, cancellationToken);

        return Result<ReleaseLetterRequestDto>.Success(
            await releaseLetters.DetailAsync(serviceRequest, clientView: true, internalView: false, organization.Name, cancellationToken));
    }

    private static void Add(JsonObject values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            values[key] = value.Trim();
    }
}

public sealed class GetReleaseLetterRequestQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ReleaseLetterService releaseLetters)
    : IQueryHandler<GetReleaseLetterRequestQuery, ReleaseLetterRequestDto>
{
    public async Task<Result<ReleaseLetterRequestDto>> Handle(GetReleaseLetterRequestQuery request, CancellationToken cancellationToken)
    {
        var own = await OwnServiceRequests.LoadAsync(dbContext, accessEvaluator, request.Id, track: false, requesterOnly: false, cancellationToken);
        if (own.IsFailure || own.Value.Request.DefinitionCode != ServiceDefinitionCodes.ReleaseLetter)
            return Result<ReleaseLetterRequestDto>.Failure(DomainErrors.ServiceRequest.NotFound(request.Id));

        var (serviceRequest, scope) = own.Value;
        var organizationName = await dbContext.Clients.AsNoTracking()
            .Where(c => c.Id == serviceRequest.OrganizationId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);
        var clientView = serviceRequest.OrganizationId == scope.OrganizationId && scope.CanOperate;

        return Result<ReleaseLetterRequestDto>.Success(
            await releaseLetters.DetailAsync(serviceRequest, clientView, internalView: false, organizationName, cancellationToken));
    }
}

public sealed class GetInternalReleaseLetterRequestQueryHandler(
    IApplicationDbContext dbContext,
    ReleaseLetterService releaseLetters)
    : IQueryHandler<GetInternalReleaseLetterRequestQuery, ReleaseLetterRequestDto>
{
    public async Task<Result<ReleaseLetterRequestDto>> Handle(GetInternalReleaseLetterRequestQuery request, CancellationToken cancellationToken)
    {
        var serviceRequest = await dbContext.ServiceRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.DefinitionCode == ServiceDefinitionCodes.ReleaseLetter, cancellationToken);
        if (serviceRequest is null)
            return Result<ReleaseLetterRequestDto>.Failure(DomainErrors.ServiceRequest.NotFound(request.Id));

        var organizationName = await dbContext.Clients.AsNoTracking()
            .Where(c => c.Id == serviceRequest.OrganizationId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);

        return Result<ReleaseLetterRequestDto>.Success(
            await releaseLetters.DetailAsync(serviceRequest, clientView: false, internalView: true, organizationName, cancellationToken));
    }
}
