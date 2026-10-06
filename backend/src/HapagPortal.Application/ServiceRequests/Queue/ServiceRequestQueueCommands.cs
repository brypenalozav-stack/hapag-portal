namespace HapagPortal.Application.ServiceRequests.Queue;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Bandeja de los equipos internos (ED, Customer Service): sin filtro de estado ni equipo muestra lo que espera
/// una acción (pendiente de aprobación y en curso).
/// </summary>
public sealed record GetServiceRequestQueueQuery(
    string? Status = null,
    string? Team = null,
    string? DefinitionCode = null,
    string? Country = null,
    string? BlNumber = null,
    Guid? OrganizationId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ServiceRequestSummaryDto>>;

public sealed record GetInternalServiceRequestQuery(Guid Id) : IQuery<ServiceRequestDetailDto>;

/// <summary>Aprobación del equipo (Drop Off por ED, M3-09): continúa al cobro con la tarifa aceptada al enviar.</summary>
public sealed record ApproveServiceRequestCommand(Guid Id, string? Notes) : ICommand<ServiceRequestDetailDto>;

/// <summary>Rechazo del equipo con su motivo, visible para el cliente.</summary>
public sealed record RejectServiceRequestCommand(Guid Id, string Reason) : ICommand<ServiceRequestDetailDto>;

/// <summary>El equipo que presta el servicio lo da por completado (con documento de salida si la definición lo exige).</summary>
public sealed record CompleteServiceRequestCommand(Guid Id, string? Notes) : ICommand<ServiceRequestDetailDto>;

/// <summary>Nota interna en la línea de tiempo, sin cambio de estado.</summary>
public sealed record AddServiceRequestNoteCommand(Guid Id, string Notes) : ICommand<ServiceRequestDetailDto>;

/// <summary>Documento de salida que adjunta el equipo interno (respuesta a la corrección, carta, etc.).</summary>
public sealed record UploadServiceRequestOutputCommand(Guid Id, string FileName, string ContentType, byte[] Content)
    : ICommand<ServiceRequestAttachmentDto>;

public sealed record GetInternalServiceRequestAttachmentQuery(Guid Id, Guid AttachmentId) : IQuery<ServiceRequestAttachmentContentDto>;

public sealed class GetServiceRequestQueueQueryValidator : AbstractValidator<GetServiceRequestQueueQuery>
{
    public GetServiceRequestQueueQueryValidator()
    {
        RuleFor(x => x.Status).Must(s => s is null || ServiceRequestStatus.All.Contains(s)).WithMessage("Unknown status.");
        RuleFor(x => x.Team).Must(t => t is null || ServiceTeams.Internal.Contains(t)).WithMessage("Team must be ED or CustomerService.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ApproveServiceRequestCommandValidator : AbstractValidator<ApproveServiceRequestCommand>
{
    public ApproveServiceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class RejectServiceRequestCommandValidator : AbstractValidator<RejectServiceRequestCommand>
{
    public RejectServiceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CompleteServiceRequestCommandValidator : AbstractValidator<CompleteServiceRequestCommand>
{
    public CompleteServiceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class AddServiceRequestNoteCommandValidator : AbstractValidator<AddServiceRequestNoteCommand>
{
    public AddServiceRequestNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(1000);
    }
}

public sealed class UploadServiceRequestOutputCommandValidator : AbstractValidator<UploadServiceRequestOutputCommand>
{
    public UploadServiceRequestOutputCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .Must(c => UploadServiceRequestAttachmentCommandValidator.AllowedContentTypes.Contains(c))
            .WithMessage("Only PDF, PNG or JPEG files are accepted.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("The file is empty.")
            .Must(c => c.Length <= UploadServiceRequestAttachmentCommandValidator.MaxSizeBytes).WithMessage("The file must not exceed 10 MB.");
    }
}

internal static class InternalServiceRequests
{
    public static async Task<Result<(ServiceRequest Request, ServiceDefinition Definition)>> LoadAsync(
        IApplicationDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null)
            return Result<(ServiceRequest, ServiceDefinition)>.Failure(DomainErrors.ServiceRequest.NotFound(id));

        var definition = await dbContext.ServiceDefinitions.AsNoTracking().FirstAsync(d => d.Id == request.DefinitionId, cancellationToken);
        return Result<(ServiceRequest, ServiceDefinition)>.Success((request, definition));
    }

    /// <summary>Guarda, avisa a la organización solicitante y devuelve el detalle.</summary>
    public static async Task<Result<ServiceRequestDetailDto>> SaveAndNotifyAsync(
        IApplicationDbContext dbContext,
        ServiceRequestWorkflow workflow,
        ServiceRequest request,
        ServiceDefinition definition,
        bool notify,
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        if (notify)
            await workflow.NotifyAsync(request, definition.NameEs, cancellationToken);

        return Result<ServiceRequestDetailDto>.Success(
            await ServiceRequestViews.DetailAsync(dbContext, request, clientView: false, cancellationToken));
    }
}

public sealed class GetServiceRequestQueueQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetServiceRequestQueueQuery, PagedResult<ServiceRequestSummaryDto>>
{
    private static readonly string[] Actionable = [ServiceRequestStatus.PendingApproval, ServiceRequestStatus.InProgress];

    public async Task<Result<PagedResult<ServiceRequestSummaryDto>>> Handle(GetServiceRequestQueueQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ServiceRequests.AsNoTracking()
            .Where(r => r.Status != ServiceRequestStatus.Draft);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);
        else if (string.IsNullOrWhiteSpace(request.BlNumber) && request.OrganizationId is null)
            query = query.Where(r => Actionable.Contains(r.Status));

        if (!string.IsNullOrWhiteSpace(request.Team))
            query = query.Where(r => r.AssignedTeam == request.Team);
        if (!string.IsNullOrWhiteSpace(request.DefinitionCode))
        {
            var code = request.DefinitionCode.Trim().ToUpperInvariant();
            query = query.Where(r => r.DefinitionCode == code);
        }
        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(r => r.Country == country);
        }
        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var number = request.BlNumber.Trim();
            query = query.Where(r => r.BlNumber == number || r.BookingNumber == number);
        }
        if (request.OrganizationId is { } organizationId)
            query = query.Where(r => r.OrganizationId == organizationId);

        var total = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderBy(r => r.StatusChangedAt)
            .ThenBy(r => r.RequestNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = await ServiceRequestViews.SummariesAsync(dbContext, page, cancellationToken);
        return Result<PagedResult<ServiceRequestSummaryDto>>.Success(
            new PagedResult<ServiceRequestSummaryDto>(items, total, request.Page, request.PageSize));
    }
}

public sealed class GetInternalServiceRequestQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetInternalServiceRequestQuery, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(GetInternalServiceRequestQuery request, CancellationToken cancellationToken)
    {
        var serviceRequest = await dbContext.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        return serviceRequest is null
            ? Result<ServiceRequestDetailDto>.Failure(DomainErrors.ServiceRequest.NotFound(request.Id))
            : Result<ServiceRequestDetailDto>.Success(await ServiceRequestViews.DetailAsync(dbContext, serviceRequest, clientView: false, cancellationToken));
    }
}

public sealed class ApproveServiceRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<ApproveServiceRequestCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(ApproveServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var loaded = await InternalServiceRequests.LoadAsync(dbContext, request.Id, cancellationToken);
        if (loaded.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(loaded.Error);

        var (serviceRequest, definition) = loaded.Value;
        var approved = await workflow.ApproveAsync(
            serviceRequest, definition, ServiceActor.Internal(currentUserService), request.Notes, DateTime.UtcNow, cancellationToken);
        if (approved.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(approved.Error);

        return await InternalServiceRequests.SaveAndNotifyAsync(dbContext, workflow, serviceRequest, definition, notify: true, cancellationToken);
    }
}

public sealed class RejectServiceRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<RejectServiceRequestCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(RejectServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var loaded = await InternalServiceRequests.LoadAsync(dbContext, request.Id, cancellationToken);
        if (loaded.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(loaded.Error);

        var (serviceRequest, definition) = loaded.Value;
        var rejected = ServiceRequestWorkflow.Transition(
            dbContext, serviceRequest, ServiceRequestStatus.Rejected, ServiceActor.Internal(currentUserService), request.Reason, DateTime.UtcNow);
        if (rejected.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(rejected.Error);

        serviceRequest.AssignedTeam = null;
        serviceRequest.ResolutionNotes = request.Reason.Trim();

        return await InternalServiceRequests.SaveAndNotifyAsync(dbContext, workflow, serviceRequest, definition, notify: true, cancellationToken);
    }
}

public sealed class CompleteServiceRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<CompleteServiceRequestCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(CompleteServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var loaded = await InternalServiceRequests.LoadAsync(dbContext, request.Id, cancellationToken);
        if (loaded.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(loaded.Error);

        var (serviceRequest, definition) = loaded.Value;
        if (serviceRequest.Status != ServiceRequestStatus.InProgress)
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.ServiceRequest.InvalidTransition(serviceRequest.Status, ServiceRequestStatus.Completed));

        if (definition.RequiresOutputDocument
            && !await dbContext.ServiceRequestAttachments.AsNoTracking().AnyAsync(
                a => a.ServiceRequestId == serviceRequest.Id && a.FieldKey == ServiceRequestAttachmentFields.Output, cancellationToken))
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.ServiceRequest.OutputDocumentRequired);

        // M3-11: la refacturación se completa sola al emitir la nueva factura (pago y aceptación).
        if (serviceRequest.DefinitionCode == ServiceDefinitionCodes.IaoReinvoicing)
            return Result<ServiceRequestDetailDto>.Failure(DomainErrors.Reinvoicing.InvoiceNotIssued);

        var completed = ServiceRequestWorkflow.Transition(
            dbContext, serviceRequest, ServiceRequestStatus.Completed, ServiceActor.Internal(currentUserService), request.Notes, DateTime.UtcNow);
        if (completed.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(completed.Error);

        serviceRequest.AssignedTeam = null;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            serviceRequest.ResolutionNotes = request.Notes.Trim();

        return await InternalServiceRequests.SaveAndNotifyAsync(dbContext, workflow, serviceRequest, definition, notify: true, cancellationToken);
    }
}

public sealed class AddServiceRequestNoteCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ServiceRequestWorkflow workflow)
    : ICommandHandler<AddServiceRequestNoteCommand, ServiceRequestDetailDto>
{
    public async Task<Result<ServiceRequestDetailDto>> Handle(AddServiceRequestNoteCommand request, CancellationToken cancellationToken)
    {
        var loaded = await InternalServiceRequests.LoadAsync(dbContext, request.Id, cancellationToken);
        if (loaded.IsFailure)
            return Result<ServiceRequestDetailDto>.Failure(loaded.Error);

        var (serviceRequest, definition) = loaded.Value;
        ServiceRequestWorkflow.AddEvent(
            dbContext, serviceRequest, serviceRequest.Status, serviceRequest.Status, ServiceActor.Internal(currentUserService), request.Notes, DateTime.UtcNow);

        return await InternalServiceRequests.SaveAndNotifyAsync(dbContext, workflow, serviceRequest, definition, notify: false, cancellationToken);
    }
}

public sealed class UploadServiceRequestOutputCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage)
    : ICommandHandler<UploadServiceRequestOutputCommand, ServiceRequestAttachmentDto>
{
    public async Task<Result<ServiceRequestAttachmentDto>> Handle(UploadServiceRequestOutputCommand request, CancellationToken cancellationToken)
    {
        var serviceRequest = await dbContext.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (serviceRequest is null)
            return Result<ServiceRequestAttachmentDto>.Failure(DomainErrors.ServiceRequest.NotFound(request.Id));
        if (serviceRequest.Status is ServiceRequestStatus.Draft or ServiceRequestStatus.Cancelled or ServiceRequestStatus.Rejected)
            return Result<ServiceRequestAttachmentDto>.Failure(DomainErrors.ServiceRequest.NotAssignedToTeam);

        return await ServiceRequestAttachments.StoreAsync(
            dbContext, fileStorage, serviceRequest, ServiceRequestAttachmentFields.Output, request.FileName, request.ContentType,
            request.Content, currentUserService, cancellationToken);
    }
}

public sealed class GetInternalServiceRequestAttachmentQueryHandler(
    IApplicationDbContext dbContext,
    IFileStorage fileStorage)
    : IQueryHandler<GetInternalServiceRequestAttachmentQuery, ServiceRequestAttachmentContentDto>
{
    public Task<Result<ServiceRequestAttachmentContentDto>> Handle(GetInternalServiceRequestAttachmentQuery request, CancellationToken cancellationToken) =>
        ServiceRequestAttachments.ReadAsync(dbContext, fileStorage, request.Id, request.AttachmentId, cancellationToken);
}
