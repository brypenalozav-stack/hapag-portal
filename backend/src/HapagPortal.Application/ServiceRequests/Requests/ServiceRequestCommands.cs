namespace HapagPortal.Application.ServiceRequests.Requests;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Servicios on demand aplicables a un BL o booking (M2-03, M2-04): definiciones activas del país y la
/// operación cuya acción de M1-11 el usuario puede ver, con su disponibilidad (condiciones del embarque) y el
/// cobro estimado si se enviara ahora. Por defecto solo los disponibles.
/// </summary>
public sealed record GetAvailableServicesQuery(string? BlNumber, string? BookingNumber, bool IncludeUnavailable = false)
    : IQuery<AvailableServicesDto>;

/// <summary>Cobro del servicio si se enviara ahora con los datos indicados (para aceptar la tarifa, M3-09).</summary>
public sealed record QuoteServiceRequestQuery(string DefinitionCode, string? BlNumber, string? BookingNumber, string? InputValuesJson)
    : IQuery<ServiceQuoteDto>;

/// <summary>
/// Crea una solicitud en borrador y, con <c>Submit</c>, la envía en el mismo paso (si el formulario no pide
/// archivos, que se adjuntan al borrador).
/// </summary>
public sealed record CreateServiceRequestCommand(
    string DefinitionCode,
    string? BlNumber,
    string? BookingNumber,
    string? InputValuesJson,
    ServiceBillingInput? Billing,
    bool Submit = false,
    bool AcceptTariff = false,
    decimal? AcceptedTotal = null) : ICommand<ServiceRequestDetailDto>;

public sealed record UpdateServiceRequestCommand(Guid Id, string? InputValuesJson, ServiceBillingInput? Billing)
    : ICommand<ServiceRequestDetailDto>;

public sealed record SubmitServiceRequestCommand(Guid Id, bool AcceptTariff = false, decimal? AcceptedTotal = null)
    : ICommand<ServiceRequestDetailDto>;

public sealed record CancelServiceRequestCommand(Guid Id, string? Reason) : ICommand<ServiceRequestDetailDto>;

public sealed record UploadServiceRequestAttachmentCommand(
    Guid Id,
    string FieldKey,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<ServiceRequestAttachmentDto>;

public sealed record GetServiceRequestAttachmentQuery(Guid Id, Guid AttachmentId) : IQuery<ServiceRequestAttachmentContentDto>;

public sealed record ServiceRequestAttachmentContentDto(byte[] Content, string ContentType, string FileName);

public sealed record GetMyServiceRequestsQuery(
    string? Status = null,
    string? DefinitionCode = null,
    string? BlNumber = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ServiceRequestSummaryDto>>;

public sealed record GetServiceRequestQuery(Guid Id) : IQuery<ServiceRequestDetailDto>;

public sealed class GetAvailableServicesQueryValidator : AbstractValidator<GetAvailableServicesQuery>
{
    public GetAvailableServicesQueryValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.BlNumber) || !string.IsNullOrWhiteSpace(x.BookingNumber))
            .WithName("BlNumber").WithMessage("Indicate the BL or booking number.");
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.BookingNumber).MaximumLength(50);
    }
}

public sealed class QuoteServiceRequestQueryValidator : AbstractValidator<QuoteServiceRequestQuery>
{
    public QuoteServiceRequestQueryValidator()
    {
        RuleFor(x => x.DefinitionCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.BlNumber) || !string.IsNullOrWhiteSpace(x.BookingNumber))
            .WithName("BlNumber").WithMessage("Indicate the BL or booking number.");
        RuleFor(x => x.InputValuesJson).MaximumLength(20000);
    }
}

public sealed class CreateServiceRequestCommandValidator : AbstractValidator<CreateServiceRequestCommand>
{
    public CreateServiceRequestCommandValidator()
    {
        RuleFor(x => x.DefinitionCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.BlNumber) || !string.IsNullOrWhiteSpace(x.BookingNumber))
            .WithName("BlNumber").WithMessage("Indicate the BL or booking number.");
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.BookingNumber).MaximumLength(50);
        RuleFor(x => x.InputValuesJson).MaximumLength(20000);
        RuleFor(x => x.AcceptedTotal).GreaterThanOrEqualTo(0m).When(x => x.AcceptedTotal is not null);
        RuleFor(x => x.Billing!).SetValidator(new ServiceBillingInputValidator()).When(x => x.Billing is not null);
    }
}

public sealed class UpdateServiceRequestCommandValidator : AbstractValidator<UpdateServiceRequestCommand>
{
    public UpdateServiceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InputValuesJson).MaximumLength(20000);
        RuleFor(x => x.Billing!).SetValidator(new ServiceBillingInputValidator()).When(x => x.Billing is not null);
    }
}

public sealed class ServiceBillingInputValidator : AbstractValidator<ServiceBillingInput>
{
    public ServiceBillingInputValidator()
    {
        RuleFor(x => x.TaxId).MaximumLength(20);
        RuleFor(x => x.Name).MaximumLength(ServiceRequestWorkflow.MaxBillingNameLength);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Activity).MaximumLength(200);
    }
}

public sealed class SubmitServiceRequestCommandValidator : AbstractValidator<SubmitServiceRequestCommand>
{
    public SubmitServiceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AcceptedTotal).GreaterThanOrEqualTo(0m).When(x => x.AcceptedTotal is not null);
    }
}

public sealed class CancelServiceRequestCommandValidator : AbstractValidator<CancelServiceRequestCommand>
{
    public CancelServiceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class UploadServiceRequestAttachmentCommandValidator : AbstractValidator<UploadServiceRequestAttachmentCommand>
{
    public const int MaxSizeBytes = 10 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes = ["application/pdf", "image/png", "image/jpeg"];

    public UploadServiceRequestAttachmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FieldKey).NotEmpty().MaximumLength(40);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .Must(c => AllowedContentTypes.Contains(c))
            .WithMessage("Only PDF, PNG or JPEG files are accepted.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("The file is empty.")
            .Must(c => c.Length <= MaxSizeBytes).WithMessage("The file must not exceed 10 MB.");
    }
}

public sealed class GetMyServiceRequestsQueryValidator : AbstractValidator<GetMyServiceRequestsQuery>
{
    public GetMyServiceRequestsQueryValidator()
    {
        RuleFor(x => x.Status).Must(s => s is null || ServiceRequestStatus.All.Contains(s)).WithMessage("Unknown status.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

/// <summary>Solicitudes de la propia organización (como solicitante o como mandante).</summary>
internal static class OwnServiceRequests
{
    public static async Task<Result<(ServiceRequest Request, AccessScope Scope)>> LoadAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        Guid id,
        bool track,
        bool requesterOnly,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (!scope.IsOperational || scope.OrganizationId is null)
            return Result<(ServiceRequest, AccessScope)>.Failure(Error.Forbidden);

        var organizationId = scope.OrganizationId.Value;
        var query = track ? dbContext.ServiceRequests : dbContext.ServiceRequests.AsNoTracking();
        var request = await query.FirstOrDefaultAsync(
            r => r.Id == id && (r.OrganizationId == organizationId || (!requesterOnly && r.OnBehalfOfClientId == organizationId)),
            cancellationToken);

        return request is null
            ? Result<(ServiceRequest, AccessScope)>.Failure(DomainErrors.ServiceRequest.NotFound(id))
            : Result<(ServiceRequest, AccessScope)>.Success((request, scope));
    }
}

public sealed class GetAvailableServicesQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ServiceCatalogEvaluator catalog)
    : IQueryHandler<GetAvailableServicesQuery, AvailableServicesDto>
{
    public async Task<Result<AvailableServicesDto>> Handle(GetAvailableServicesQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ServiceShipmentLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, request.BookingNumber, forOperation: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<AvailableServicesDto>.Failure(loaded.Error);

        var (bl, permissions, organization, _) = loaded.Value;
        var now = DateTime.UtcNow;

        var containers = await dbContext.BLContainers.AsNoTracking()
            .Where(c => c.BillOfLadingId == bl.Id)
            .OrderBy(c => c.ContainerNumber)
            .ToListAsync(cancellationToken);

        var definitions = (await dbContext.ServiceDefinitions.AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Code)
                .ToListAsync(cancellationToken))
            .Where(d => ServiceCatalogEvaluator.Applies(d, bl, permissions))
            .ToList();

        var services = new List<AvailableServiceDto>();
        foreach (var definition in definitions)
        {
            var availability = await catalog.EvaluateAsync(definition, bl, containers.Count, now, null, cancellationToken);
            if (!availability.Available && !request.IncludeUnavailable)
                continue;

            // Estimación con todos los contenedores (o sin datos): el valor final se fija al enviar.
            ServiceQuoteDto? quote = null;
            string? quoteError = null;
            if (availability.Available && definition.PricingMode != ServicePricingModes.None)
            {
                var numbers = new Dictionary<string, decimal>();
                var computed = await catalog.QuoteAsync(
                    new ServiceQuoteInput(definition, bl, organization, permissions,
                        containers.Select(c => c.ContainerNumber.ToUpperInvariant()).ToList(), numbers, now),
                    cancellationToken);
                if (computed.IsSuccess)
                    quote = computed.Value.Quote;
                else
                    quoteError = computed.Error.Code;
            }

            var reasons = availability.Reasons.ToList();
            var canRequest = availability.Available && permissions.CanExecute(definition.ActionCode) && !permissions.RequiresAssociationForPayment;
            if (availability.Available && !permissions.CanExecute(definition.ActionCode))
                reasons.Add(ServiceUnavailableReasons.NoPermission);

            services.Add(new AvailableServiceDto(
                definition.Id,
                definition.Code,
                definition.NameEs,
                definition.NameEn,
                definition.DescriptionEs,
                definition.DescriptionEn,
                definition.ReferenceType,
                availability.Available,
                canRequest,
                reasons,
                ServiceInputSchema.Parse(definition.InputSchemaJson) ?? [],
                definition.BillingDataRequired,
                definition.TariffAcceptanceRequired,
                definition.ApprovalTeam,
                definition.FulfillmentTeam,
                quote,
                quoteError));
        }

        return Result<AvailableServicesDto>.Success(new AvailableServicesDto(
            bl.Id,
            bl.BLNumber,
            bl.BookingNumber,
            bl.Country,
            ServiceOperations.Of(bl.ShipmentType),
            bl.Status,
            bl.ETD,
            bl.ETA,
            BusinessCalendar.TimeZoneId(bl.Country),
            containers.Select(c => new ShipmentContainerOptionDto(c.ContainerNumber, c.ContainerType, c.Status, c.IsShipperOwned)).ToList(),
            services,
            now));
    }
}

public sealed class QuoteServiceRequestQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ServiceCatalogEvaluator catalog,
    ServiceRequestWorkflow workflow)
    : IQueryHandler<QuoteServiceRequestQuery, ServiceQuoteDto>
{
    public async Task<Result<ServiceQuoteDto>> Handle(QuoteServiceRequestQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ServiceShipmentLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, request.BookingNumber, forOperation: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<ServiceQuoteDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var code = request.DefinitionCode.Trim().ToUpperInvariant();
        var definition = await dbContext.ServiceDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code && d.IsActive, cancellationToken);
        if (definition is null || !ServiceCatalogEvaluator.Applies(definition, context.BillOfLading, context.Permissions))
            return Result<ServiceQuoteDto>.Failure(DomainErrors.ServiceDefinition.NotFoundByCode(code));

        var input = await workflow.ValidateInputAsync(definition, context.BillOfLading, null, request.InputValuesJson, requireComplete: false, cancellationToken);
        if (input.IsFailure)
            return ServiceRequestViews.Fail<ServiceQuoteDto>(input);

        var computed = await catalog.QuoteAsync(
            new ServiceQuoteInput(definition, context.BillOfLading, context.Organization, context.Permissions,
                input.Value.Containers, input.Value.Numbers, DateTime.UtcNow),
            cancellationToken);

        return computed.IsSuccess
            ? Result<ServiceQuoteDto>.Success(computed.Value.Quote)
            : Result<ServiceQuoteDto>.Failure(computed.Error);
    }
}

public sealed class CreateServiceRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    ServiceCatalogEvaluator catalog,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<CreateServiceRequestCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(CreateServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ServiceShipmentLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, request.BookingNumber, forOperation: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var code = request.DefinitionCode.Trim().ToUpperInvariant();
        var definition = await dbContext.ServiceDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code && d.IsActive, cancellationToken);
        if (definition is null || !ServiceCatalogEvaluator.Applies(definition, context.BillOfLading, context.Permissions))
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.ServiceDefinition.NotFoundByCode(code));

        // M1-11: solicitar exige la acción de la definición y un perfil que opere; M1-18: asociarse antes.
        if (!context.Permissions.CanExecute(definition.ActionCode))
            return Result<ServiceRequestDetailDto>.Failure(Error.Forbidden);
        if (context.Permissions.RequiresAssociationForPayment)
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.Cart.AssociationRequired);

        var now = DateTime.UtcNow;
        var containerCount = await dbContext.BLContainers.AsNoTracking().CountAsync(c => c.BillOfLadingId == context.BillOfLading.Id, cancellationToken);
        var availability = await catalog.EvaluateAsync(definition, context.BillOfLading, containerCount, now, null, cancellationToken);
        if (!availability.Available)
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.ServiceRequest.NotAvailable(string.Join(", ", availability.Reasons)));

        var input = await workflow.ValidateInputAsync(definition, context.BillOfLading, null, request.InputValuesJson, requireComplete: false, cancellationToken);
        if (input.IsFailure)
            return ServiceRequestViews.Fail<ServiceRequestDetailDto>(input);

        var billing = request.Billing ?? new ServiceBillingInput(null, null, null, null, null);
        var billingCheck = await workflow.ValidateBillingAsync(definition, billing, context.Organization, context.Permissions, requireComplete: false, cancellationToken);
        if (billingCheck.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(billingCheck.Error);

        var bl = context.BillOfLading;
        var grant = context.Permissions.GrantFor(definition.ActionCode);
        var serviceRequest = new ServiceRequest
        {
            RequestNumber = PaymentLifecycle.NewNumber(DocumentPrefixes.ServiceRequest, now),
            DefinitionId = definition.Id,
            DefinitionCode = definition.Code,
            OrganizationId = context.Organization.Id,
            RequestedByUserId = currentUserService.UserId,
            RequestedByEmail = currentUserService.Email,
            OnBehalfOfClientId = grant?.GrantorOrganizationId,
            AccessGrantId = grant?.GrantId,
            BillOfLadingId = bl.Id,
            BlNumber = bl.BLNumber,
            BookingNumber = bl.BookingNumber,
            Country = bl.Country,
            Operation = ServiceOperations.Of(bl.ShipmentType),
            InputValuesJson = input.Value.NormalizedJson,
            ContainerNumbers = input.Value.Containers.Count == 0 ? null : string.Join(',', input.Value.Containers),
            Status = ServiceRequestStatus.Draft,
            StatusChangedAt = now,
            CreatedAt = now
        };
        ServiceRequestWorkflow.WriteBilling(serviceRequest, billing);
        dbContext.ServiceRequests.Add(serviceRequest);

        var actor = ServiceActor.Client(currentUserService);
        ServiceRequestWorkflow.AddEvent(dbContext, serviceRequest, null, ServiceRequestStatus.Draft, actor, null, now);

        if (request.Submit)
        {
            var submitted = await workflow.SubmitAsync(
                serviceRequest, definition, context, actor, request.AcceptTariff, request.AcceptedTotal, now, cancellationToken);
            if (submitted.IsFailure)
                return ServiceRequestViews.Fail<ServiceRequestDetailDto>(submitted);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await workflow.NotifyAsync(serviceRequest, definition.NameEs, cancellationToken);

        return Result<ServiceRequestDetailDto>.Success(
            await ServiceRequestViews.DetailAsync(dbContext, serviceRequest, clientView: true, cancellationToken));
    }
}

public sealed class UpdateServiceRequestCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<UpdateServiceRequestCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(UpdateServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var own = await OwnServiceRequests.LoadAsync(dbContext, accessEvaluator, request.Id, track: true, requesterOnly: true, cancellationToken);
        if (own.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(own.Error);

        var serviceRequest = own.Value.Request;
        if (serviceRequest.Status != ServiceRequestStatus.Draft)
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.ServiceRequest.NotEditable);

        var context = await ServiceShipmentLoader.LoadByIdAsync(dbContext, accessEvaluator, serviceRequest.BillOfLadingId, cancellationToken);
        if (context.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(context.Error);

        var definition = await dbContext.ServiceDefinitions.AsNoTracking().FirstAsync(d => d.Id == serviceRequest.DefinitionId, cancellationToken);
        if (!context.Value.Permissions.CanExecute(definition.ActionCode))
            return Result<ServiceRequestDetailDto>.Failure(Error.Forbidden);

        if (request.InputValuesJson is not null)
        {
            var input = await workflow.ValidateInputAsync(
                definition, context.Value.BillOfLading, serviceRequest.Id, request.InputValuesJson, requireComplete: false, cancellationToken);
            if (input.IsFailure)
                return ServiceRequestViews.Fail<ServiceRequestDetailDto>(input);

            serviceRequest.InputValuesJson = input.Value.NormalizedJson;
            serviceRequest.ContainerNumbers = input.Value.Containers.Count == 0 ? null : string.Join(',', input.Value.Containers);
        }

        if (request.Billing is not null)
        {
            var billing = await workflow.ValidateBillingAsync(
                definition, request.Billing, context.Value.Organization, context.Value.Permissions, requireComplete: false, cancellationToken);
            if (billing.IsFailure)
                return Result<ServiceRequestDetailDto>.Failure(billing.Error);

            ServiceRequestWorkflow.WriteBilling(serviceRequest, request.Billing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ServiceRequestDetailDto>.Success(
            await ServiceRequestViews.DetailAsync(dbContext, serviceRequest, clientView: true, cancellationToken));
    }
}

public sealed class SubmitServiceRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<SubmitServiceRequestCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(SubmitServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var own = await OwnServiceRequests.LoadAsync(dbContext, accessEvaluator, request.Id, track: true, requesterOnly: true, cancellationToken);
        if (own.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(own.Error);

        var serviceRequest = own.Value.Request;
        var context = await ServiceShipmentLoader.LoadByIdAsync(dbContext, accessEvaluator, serviceRequest.BillOfLadingId, cancellationToken);
        if (context.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(context.Error);

        var definition = await dbContext.ServiceDefinitions.AsNoTracking().FirstAsync(d => d.Id == serviceRequest.DefinitionId, cancellationToken);
        if (!definition.IsActive)
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.ServiceDefinition.NotFoundByCode(definition.Code));
        if (!context.Value.Permissions.CanExecute(definition.ActionCode))
            return Result<ServiceRequestDetailDto>.Failure(Error.Forbidden);

        var submitted = await workflow.SubmitAsync(
            serviceRequest, definition, context.Value, ServiceActor.Client(currentUserService),
            request.AcceptTariff, request.AcceptedTotal, DateTime.UtcNow, cancellationToken);
        if (submitted.IsFailure)
            return ServiceRequestViews.Fail<ServiceRequestDetailDto>(submitted);

        await dbContext.SaveChangesAsync(cancellationToken);
        await workflow.NotifyAsync(serviceRequest, definition.NameEs, cancellationToken);

        return Result<ServiceRequestDetailDto>.Success(
            await ServiceRequestViews.DetailAsync(dbContext, serviceRequest, clientView: true, cancellationToken));
    }
}

public sealed class CancelServiceRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<CancelServiceRequestCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(CancelServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var own = await OwnServiceRequests.LoadAsync(dbContext, accessEvaluator, request.Id, track: true, requesterOnly: true, cancellationToken);
        if (own.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(own.Error);
        if (!own.Value.Scope.CanOperate)
            return Result<ServiceRequestDetailDto>.Failure(Error.Forbidden);

        var serviceRequest = own.Value.Request;
        var cancelled = await workflow.CancelAsync(serviceRequest, ServiceActor.Client(currentUserService), request.Reason, DateTime.UtcNow, cancellationToken);
        if (cancelled.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(cancelled.Error);

        await dbContext.SaveChangesAsync(cancellationToken);

        var name = await dbContext.ServiceDefinitions.AsNoTracking()
            .Where(d => d.Id == serviceRequest.DefinitionId).Select(d => d.NameEs).FirstAsync(cancellationToken);
        await workflow.NotifyAsync(serviceRequest, name, cancellationToken);

        return Result<ServiceRequestDetailDto>.Success(
            await ServiceRequestViews.DetailAsync(dbContext, serviceRequest, clientView: true, cancellationToken));
    }
}

public sealed class UploadServiceRequestAttachmentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    IFileStorage fileStorage)
    : ICommandHandler<UploadServiceRequestAttachmentCommand, ServiceRequestAttachmentDto>
{
    public const string StorageContainer = "service-requests";

    public async Task<Result<ServiceRequestAttachmentDto>> Handle(UploadServiceRequestAttachmentCommand request, CancellationToken cancellationToken)
    {
        var own = await OwnServiceRequests.LoadAsync(dbContext, accessEvaluator, request.Id, track: false, requesterOnly: true, cancellationToken);
        if (own.IsFailure)
            return Result<ServiceRequestAttachmentDto>.Failure(own.Error);
        if (!own.Value.Scope.CanOperate)
            return Result<ServiceRequestAttachmentDto>.Failure(Error.Forbidden);

        var serviceRequest = own.Value.Request;
        if (serviceRequest.Status != ServiceRequestStatus.Draft)
            return Result<ServiceRequestAttachmentDto>.Failure(DomainErrors.ServiceRequest.NotEditable);

        var definition = await dbContext.ServiceDefinitions.AsNoTracking().FirstAsync(d => d.Id == serviceRequest.DefinitionId, cancellationToken);
        var fields = ServiceInputSchema.Parse(definition.InputSchemaJson) ?? [];
        if (!fields.Any(f => f.Key == request.FieldKey && f.Type == ServiceInputFieldTypes.File))
            return Result<ServiceRequestAttachmentDto>.Failure(DomainErrors.ServiceRequest.UnknownFileField);

        return await ServiceRequestAttachments.StoreAsync(
            dbContext, fileStorage, serviceRequest, request.FieldKey, request.FileName, request.ContentType, request.Content,
            currentUserService, cancellationToken);
    }
}

/// <summary>Guardado de adjuntos por el puerto de almacenamiento (CT-STORAGE) y lectura de su contenido.</summary>
public static class ServiceRequestAttachments
{
    public static async Task<Result<ServiceRequestAttachmentDto>> StoreAsync(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage,
        ServiceRequest serviceRequest,
        string fieldKey,
        string fileName,
        string contentType,
        byte[] content,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content, writable: false);
        var stored = await fileStorage.SaveAsync(
            stream, fileName, contentType, UploadServiceRequestAttachmentCommandHandler.StorageContainer, cancellationToken);
        if (stored.IsFailure)
            return Result<ServiceRequestAttachmentDto>.Failure(stored.Error);

        var attachment = new ServiceRequestAttachment
        {
            ServiceRequestId = serviceRequest.Id,
            FieldKey = fieldKey,
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            SizeBytes = content.LongLength,
            StorageKey = stored.Value,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = currentUserService.UserId,
            UploadedBy = currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "user"
        };

        dbContext.ServiceRequestAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ServiceRequestAttachmentDto>.Success(ServiceRequestViews.ToDto(attachment));
    }

    public static async Task<Result<ServiceRequestAttachmentContentDto>> ReadAsync(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage,
        Guid requestId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await dbContext.ServiceRequestAttachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ServiceRequestId == requestId, cancellationToken);
        if (attachment is null)
            return Result<ServiceRequestAttachmentContentDto>.Failure(DomainErrors.ServiceRequest.AttachmentNotFound(attachmentId));

        var opened = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        if (opened.IsFailure)
            return Result<ServiceRequestAttachmentContentDto>.Failure(opened.Error);
        if (opened.Value is null)
            return Result<ServiceRequestAttachmentContentDto>.Failure(DomainErrors.ServiceRequest.AttachmentNotFound(attachmentId));

        await using var content = opened.Value;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        return Result<ServiceRequestAttachmentContentDto>.Success(
            new ServiceRequestAttachmentContentDto(buffer.ToArray(), attachment.ContentType, attachment.FileName));
    }
}

public sealed class GetServiceRequestAttachmentQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IFileStorage fileStorage)
    : IQueryHandler<GetServiceRequestAttachmentQuery, ServiceRequestAttachmentContentDto>
{
    public async Task<Result<ServiceRequestAttachmentContentDto>> Handle(GetServiceRequestAttachmentQuery request, CancellationToken cancellationToken)
    {
        var own = await OwnServiceRequests.LoadAsync(dbContext, accessEvaluator, request.Id, track: false, requesterOnly: false, cancellationToken);
        if (own.IsFailure)
            return Result<ServiceRequestAttachmentContentDto>.Failure(own.Error);

        return await ServiceRequestAttachments.ReadAsync(dbContext, fileStorage, request.Id, request.AttachmentId, cancellationToken);
    }
}

public sealed class GetMyServiceRequestsQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetMyServiceRequestsQuery, PagedResult<ServiceRequestSummaryDto>>
{
    public async Task<Result<PagedResult<ServiceRequestSummaryDto>>> Handle(GetMyServiceRequestsQuery request, CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (!scope.IsOperational || scope.OrganizationId is null)
            return Result<PagedResult<ServiceRequestSummaryDto>>.Failure(Error.Forbidden);

        var organizationId = scope.OrganizationId.Value;
        var query = dbContext.ServiceRequests.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId || r.OnBehalfOfClientId == organizationId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.DefinitionCode))
        {
            var code = request.DefinitionCode.Trim().ToUpperInvariant();
            query = query.Where(r => r.DefinitionCode == code);
        }
        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var number = request.BlNumber.Trim();
            query = query.Where(r => r.BlNumber == number || r.BookingNumber == number);
        }

        var total = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RequestNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = await ServiceRequestViews.SummariesAsync(dbContext, page, cancellationToken);
        return Result<PagedResult<ServiceRequestSummaryDto>>.Success(
            new PagedResult<ServiceRequestSummaryDto>(items, total, request.Page, request.PageSize));
    }
}

public sealed class GetServiceRequestQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetServiceRequestQuery, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(GetServiceRequestQuery request, CancellationToken cancellationToken)
    {
        var own = await OwnServiceRequests.LoadAsync(dbContext, accessEvaluator, request.Id, track: false, requesterOnly: false, cancellationToken);
        if (own.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(own.Error);

        var clientView = own.Value.Request.OrganizationId == own.Value.Scope.OrganizationId && own.Value.Scope.CanOperate;
        return Result<ServiceRequestDetailDto>.Success(
            await ServiceRequestViews.DetailAsync(dbContext, own.Value.Request, clientView, cancellationToken));
    }
}
