namespace HapagPortal.Application.Organizations.ParentCompany;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Vínculo filial–empresa matriz (M1-21).</summary>
public sealed record ParentLinkDto(
    Guid Id,
    OrganizationRefDto Organization,
    OrganizationRefDto Parent,
    string Status,
    bool VisibilityEnabled,
    DateTime? VisibilityChangedAt,
    string? VisibilityChangedBy,
    DateTime RequestedAt,
    string RequestedBy,
    string? Notes,
    DateTime? DecidedAt,
    string? DecidedBy,
    string? DecisionNotes,
    DateTime? EndedAt,
    string? EndedBy);

/// <summary>
/// Situación de la organización: su vínculo con una empresa matriz (pendiente o activo), las filiales que le comparten
/// sus BL como matriz y si el usuario puede administrarlo (M1-11: X para agencias y transportistas).
/// </summary>
public sealed record ParentCompanyDto(
    ParentLinkDto? Link,
    IReadOnlyList<ParentLinkDto> Subsidiaries,
    bool CanManage);

public sealed record ParentCandidateDto(Guid Id, string Name, string TaxId, string OrganizationType, string Country);

public sealed record GetParentCompanyQuery : IQuery<ParentCompanyDto>;

/// <summary>Empresas que pueden ser matriz: organizaciones cliente o FFWW aprobadas (máx. 20).</summary>
public sealed record GetParentCandidatesQuery(string? Search = null) : IQuery<IReadOnlyList<ParentCandidateDto>>;

/// <summary>La filial pide asociarse a su matriz; queda pendiente de la aprobación de Hapag-Lloyd.</summary>
public sealed record RequestParentLinkCommand(Guid ParentOrganizationId, bool EnableVisibility = true, string? Notes = null)
    : ICommand<ParentLinkDto>;

/// <summary>La filial activa o desactiva la visibilidad de sus BL hacia la matriz (vínculo activo).</summary>
public sealed record SetParentVisibilityCommand(bool Enabled) : ICommand<ParentLinkDto>;

/// <summary>La filial retira la solicitud o termina el vínculo; la matriz deja de ver sus BL de inmediato.</summary>
public sealed record RemoveParentLinkCommand : ICommand;

public sealed record GetParentLinksQuery(string? Status = null) : IQuery<IReadOnlyList<ParentLinkDto>>;

public sealed record ApproveParentLinkCommand(Guid Id, string? Notes = null) : ICommand<ParentLinkDto>;

public sealed record RejectParentLinkCommand(Guid Id, string Reason) : ICommand<ParentLinkDto>;

public sealed class RequestParentLinkCommandValidator : AbstractValidator<RequestParentLinkCommand>
{
    public RequestParentLinkCommandValidator()
    {
        RuleFor(x => x.ParentOrganizationId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RejectParentLinkCommandValidator : AbstractValidator<RejectParentLinkCommand>
{
    public RejectParentLinkCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class ApproveParentLinkCommandValidator : AbstractValidator<ApproveParentLinkCommand>
{
    public ApproveParentLinkCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

internal static class ParentLinks
{
    public static readonly string[] ParentTypes = [OrganizationTypes.Customer, OrganizationTypes.FreightForwarder];

    public static async Task<List<ParentLinkDto>> ToDtosAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<OrganizationParentLink> links,
        CancellationToken cancellationToken)
    {
        var organizations = await AccessGrantMapper.LoadOrganizationsAsync(
            dbContext, links.SelectMany(l => new[] { l.OrganizationId, l.ParentOrganizationId }), cancellationToken);

        return links
            .Where(l => organizations.ContainsKey(l.OrganizationId) && organizations.ContainsKey(l.ParentOrganizationId))
            .Select(l => new ParentLinkDto(
                l.Id,
                AccessGrantMapper.ToRef(organizations[l.OrganizationId]),
                AccessGrantMapper.ToRef(organizations[l.ParentOrganizationId]),
                l.Status,
                l.VisibilityEnabled,
                l.VisibilityChangedAt,
                l.VisibilityChangedBy,
                l.RequestedAt,
                l.RequestedBy,
                l.Notes,
                l.DecidedAt,
                l.DecidedBy,
                l.DecisionNotes,
                l.EndedAt,
                l.EndedBy))
            .ToList();
    }

    public static async Task<ParentLinkDto> ToDtoAsync(IApplicationDbContext dbContext, OrganizationParentLink link, CancellationToken cancellationToken) =>
        (await ToDtosAsync(dbContext, [link], cancellationToken))[0];

    /// <summary>Administrar el vínculo es una acción de la organización en M1-11 (X para agencias y transportistas).</summary>
    public static async Task<Result<AccessManagementContext>> LoadContextAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IShipmentAccessEvaluator accessEvaluator,
        bool requireOperate,
        CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(dbContext, currentUserService, accessEvaluator, requireOperate, cancellationToken);
        if (loaded.IsFailure)
            return loaded;

        if (requireOperate && !loaded.Value.Matrix.OrganizationCan(loaded.Value.OrganizationType, ShipmentActionCodes.EnableParentCompanyVisibility))
            return Result<AccessManagementContext>.Failure(DomainErrors.ParentLink.NotAllowed);

        return loaded;
    }

    public static Task<OrganizationParentLink?> OpenLinkAsync(IApplicationDbContext dbContext, Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.OrganizationParentLinks
            .Where(l => l.OrganizationId == organizationId && ParentLinkStatus.Open.Contains(l.Status))
            .OrderByDescending(l => l.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task NotifyAdminsAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        Guid organizationId,
        OrganizationParentLink link,
        string type,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == organizationId, cancellationToken);
        if (organization is null)
            return;

        await OrganizationNotifier.NotifyAdminsAsync(
            dbContext, notificationPublisher, organization, type, title, body, cancellationToken,
            dedupKeyPrefix: $"parent-link:{link.Id}:{type}:{link.VisibilityEnabled}",
            link: new NotificationLink(NotificationEntityTypes.ParentLink, link.Id.ToString(), organization.Name));
    }
}

public sealed class GetParentCompanyQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetParentCompanyQuery, ParentCompanyDto>
{
    public async Task<Result<ParentCompanyDto>> Handle(GetParentCompanyQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ParentLinks.LoadContextAsync(dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<ParentCompanyDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var organizationId = context.OrganizationId;
        var own = await dbContext.OrganizationParentLinks.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId && ParentLinkStatus.Open.Contains(l.Status))
            .OrderByDescending(l => l.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var subsidiaries = await dbContext.OrganizationParentLinks.AsNoTracking()
            .Where(l => l.ParentOrganizationId == organizationId && l.Status == ParentLinkStatus.Active)
            .OrderBy(l => l.RequestedAt)
            .ToListAsync(cancellationToken);

        var canManage = context.Scope.CanOperate
            && currentUserService.HasPermission(AccessPermissions.ManageThirdPartyAccess)
            && context.Matrix.OrganizationCan(context.OrganizationType, ShipmentActionCodes.EnableParentCompanyVisibility);

        return Result<ParentCompanyDto>.Success(new ParentCompanyDto(
            own is null ? null : await ParentLinks.ToDtoAsync(dbContext, own, cancellationToken),
            await ParentLinks.ToDtosAsync(dbContext, subsidiaries, cancellationToken),
            canManage));
    }
}

public sealed class GetParentCandidatesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetParentCandidatesQuery, IReadOnlyList<ParentCandidateDto>>
{
    public async Task<Result<IReadOnlyList<ParentCandidateDto>>> Handle(GetParentCandidatesQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ParentLinks.LoadContextAsync(dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<ParentCandidateDto>>.Failure(loaded.Error);

        var organizationId = loaded.Value.OrganizationId;
        var query = dbContext.Clients.AsNoTracking()
            .Where(c => c.Id != organizationId
                && c.IsActive
                && c.RegistrationStatus == OrganizationStatus.Approved
                && ParentLinks.ParentTypes.Contains(c.OrganizationType));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.TaxId.ToLower().Contains(term));
        }

        IReadOnlyList<ParentCandidateDto> items = await query
            .OrderBy(c => c.Name)
            .Take(20)
            .Select(c => new ParentCandidateDto(c.Id, c.Name, c.TaxId, c.OrganizationType, c.Country))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ParentCandidateDto>>.Success(items);
    }
}

public sealed class RequestParentLinkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<RequestParentLinkCommand, ParentLinkDto>
{
    public async Task<Result<ParentLinkDto>> Handle(RequestParentLinkCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ParentLinks.LoadContextAsync(dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ParentLinkDto>.Failure(loaded.Error);

        var context = loaded.Value;
        if (request.ParentOrganizationId == context.OrganizationId)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.SelfLink);

        var parent = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ParentOrganizationId, cancellationToken);
        if (parent is null || !parent.IsActive || parent.RegistrationStatus != OrganizationStatus.Approved
            || !ParentLinks.ParentTypes.Contains(parent.OrganizationType))
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.InvalidParent);

        if (await ParentLinks.OpenLinkAsync(dbContext, context.OrganizationId, cancellationToken) is not null)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.AlreadyExists);

        // Sin ciclos: la matriz no puede ser a su vez filial de esta organización.
        var cycle = await dbContext.OrganizationParentLinks.AnyAsync(l =>
            l.OrganizationId == parent.Id && l.ParentOrganizationId == context.OrganizationId && ParentLinkStatus.Open.Contains(l.Status),
            cancellationToken);
        if (cycle)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.Cycle);

        var now = DateTime.UtcNow;
        var link = new OrganizationParentLink
        {
            OrganizationId = context.OrganizationId,
            ParentOrganizationId = parent.Id,
            Status = ParentLinkStatus.Pending,
            VisibilityEnabled = request.EnableVisibility,
            VisibilityChangedAt = now,
            VisibilityChangedBy = context.Actor.Email,
            RequestedAt = now,
            RequestedByUserId = context.Actor.UserId,
            RequestedBy = context.Actor.Email ?? "system",
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        dbContext.OrganizationParentLinks.Add(link);

        AccessAudit.ForOrganization(dbContext, AccessAuditEvents.ParentLinkRequested, context.Actor, now, context.OrganizationId, parent.Id,
            new { parentLinkId = link.Id, visibilityEnabled = link.VisibilityEnabled, notes = link.Notes });
        await dbContext.SaveChangesAsync(cancellationToken);

        // Bandeja interna: Hapag-Lloyd valida que la matriz corresponde al grupo empresarial (M8-04).
        await notificationPublisher.PublishAsync(
            new NotificationRequest(
                NotificationTypes.ParentLinkRequested,
                $"Solicitud de empresa matriz: {context.Membership.Organization.Name}",
                $"{context.Membership.Organization.Name} pide asociarse a {parent.Name} como su empresa matriz. Revise la solicitud en el área de administración.",
                RoleCode: RoleCodes.Administrador,
                DedupKey: $"parent-link-requested:{link.Id}",
                Link: new NotificationLink(NotificationEntityTypes.ParentLink, link.Id.ToString(), context.Membership.Organization.Name),
                Action: new NotificationAction(NotificationActionTypes.ReviewParentLink, link.Id.ToString())),
            cancellationToken);

        return Result<ParentLinkDto>.Success(await ParentLinks.ToDtoAsync(dbContext, link, cancellationToken));
    }
}

public sealed class SetParentVisibilityCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<SetParentVisibilityCommand, ParentLinkDto>
{
    public async Task<Result<ParentLinkDto>> Handle(SetParentVisibilityCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ParentLinks.LoadContextAsync(dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ParentLinkDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var link = await ParentLinks.OpenLinkAsync(dbContext, context.OrganizationId, cancellationToken);
        if (link is null)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.NotActive);

        if (link.VisibilityEnabled != request.Enabled)
        {
            var now = DateTime.UtcNow;
            link.VisibilityEnabled = request.Enabled;
            link.VisibilityChangedAt = now;
            link.VisibilityChangedBy = context.Actor.Email;

            AccessAudit.ForOrganization(dbContext,
                request.Enabled ? AccessAuditEvents.ParentVisibilityEnabled : AccessAuditEvents.ParentVisibilityDisabled,
                context.Actor, now, context.OrganizationId, link.ParentOrganizationId, new { parentLinkId = link.Id });
            await dbContext.SaveChangesAsync(cancellationToken);

            if (link.Status == ParentLinkStatus.Active)
            {
                var name = context.Membership.Organization.Name;
                await ParentLinks.NotifyAdminsAsync(dbContext, notificationPublisher, link.ParentOrganizationId, link,
                    NotificationTypes.ParentVisibilityChanged,
                    request.Enabled ? $"{name} comparte sus embarques" : $"{name} dejó de compartir sus embarques",
                    request.Enabled
                        ? $"{name} activó la visibilidad de sus BL hacia su organización como empresa matriz."
                        : $"{name} desactivó la visibilidad de sus BL hacia su organización; dejan de mostrarse.",
                    cancellationToken);
            }
        }

        return Result<ParentLinkDto>.Success(await ParentLinks.ToDtoAsync(dbContext, link, cancellationToken));
    }
}

public sealed class RemoveParentLinkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<RemoveParentLinkCommand>
{
    public async Task<Result> Handle(RemoveParentLinkCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ParentLinks.LoadContextAsync(dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result.Failure(loaded.Error);

        var context = loaded.Value;
        var link = await ParentLinks.OpenLinkAsync(dbContext, context.OrganizationId, cancellationToken);
        if (link is null)
            return Result.Failure(DomainErrors.ParentLink.NotActive);

        var now = DateTime.UtcNow;
        link.Status = ParentLinkStatus.Removed;
        link.VisibilityEnabled = false;
        link.EndedAt = now;
        link.EndedBy = context.Actor.Email;

        AccessAudit.ForOrganization(dbContext, AccessAuditEvents.ParentLinkRemoved, context.Actor, now, context.OrganizationId,
            link.ParentOrganizationId, new { parentLinkId = link.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetParentLinksQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetParentLinksQuery, IReadOnlyList<ParentLinkDto>>
{
    public async Task<Result<IReadOnlyList<ParentLinkDto>>> Handle(GetParentLinksQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.OrganizationParentLinks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(l => l.Status == request.Status);

        var links = await query.OrderByDescending(l => l.RequestedAt).Take(500).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ParentLinkDto>>.Success(await ParentLinks.ToDtosAsync(dbContext, links, cancellationToken));
    }
}

/// <summary>
/// Hapag-Lloyd aprueba el vínculo (M1-21, M8-04): desde ese momento, si la filial mantiene la visibilidad activa, la
/// matriz ve sus BL. Se avisa a los administradores de ambas organizaciones.
/// </summary>
public sealed class ApproveParentLinkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<ApproveParentLinkCommand, ParentLinkDto>
{
    public async Task<Result<ParentLinkDto>> Handle(ApproveParentLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await dbContext.OrganizationParentLinks.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (link is null)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.NotFound(request.Id));
        if (link.Status != ParentLinkStatus.Pending)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.NotPending);

        var now = DateTime.UtcNow;
        var actor = PaymentActor.From(currentUserService);
        link.Status = ParentLinkStatus.Active;
        link.DecidedAt = now;
        link.DecidedBy = actor.Name;
        link.DecisionNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        AccessAudit.ForOrganization(dbContext, AccessAuditEvents.ParentLinkApproved,
            new AccessActor(currentUserService.UserId, currentUserService.Email, currentUserService.ClientId), now,
            link.OrganizationId, link.ParentOrganizationId, new { parentLinkId = link.Id, notes = link.DecisionNotes });
        await NotificationInbox.ResolveActionAsync(dbContext, NotificationActionTypes.ReviewParentLink, link.Id.ToString(), now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await ParentLinks.ToDtoAsync(dbContext, link, cancellationToken);
        await ParentLinks.NotifyAdminsAsync(dbContext, notificationPublisher, link.OrganizationId, link, NotificationTypes.ParentLinkApproved,
            "Empresa matriz aprobada",
            $"Hapag-Lloyd aprobó la asociación con {dto.Parent.Name} como empresa matriz." +
            (link.VisibilityEnabled ? " Sus BL ya son visibles para la matriz; puede desactivarlo cuando quiera." : " La visibilidad de sus BL está desactivada."),
            cancellationToken);
        await ParentLinks.NotifyAdminsAsync(dbContext, notificationPublisher, link.ParentOrganizationId, link, NotificationTypes.ParentLinkApproved,
            $"Filial asociada: {dto.Organization.Name}",
            $"{dto.Organization.Name} quedó asociada a su organización como filial." +
            (link.VisibilityEnabled ? " Ya puede ver sus BL en el listado de embarques, identificados por organización." : string.Empty),
            cancellationToken);

        return Result<ParentLinkDto>.Success(dto);
    }
}

public sealed class RejectParentLinkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<RejectParentLinkCommand, ParentLinkDto>
{
    public async Task<Result<ParentLinkDto>> Handle(RejectParentLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await dbContext.OrganizationParentLinks.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (link is null)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.NotFound(request.Id));
        if (link.Status != ParentLinkStatus.Pending)
            return Result<ParentLinkDto>.Failure(DomainErrors.ParentLink.NotPending);

        var now = DateTime.UtcNow;
        var actor = PaymentActor.From(currentUserService);
        link.Status = ParentLinkStatus.Rejected;
        link.VisibilityEnabled = false;
        link.DecidedAt = now;
        link.DecidedBy = actor.Name;
        link.DecisionNotes = request.Reason.Trim();

        AccessAudit.ForOrganization(dbContext, AccessAuditEvents.ParentLinkRejected,
            new AccessActor(currentUserService.UserId, currentUserService.Email, currentUserService.ClientId), now,
            link.OrganizationId, link.ParentOrganizationId, new { parentLinkId = link.Id, reason = link.DecisionNotes });
        await NotificationInbox.ResolveActionAsync(dbContext, NotificationActionTypes.ReviewParentLink, link.Id.ToString(), now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await ParentLinks.ToDtoAsync(dbContext, link, cancellationToken);
        await ParentLinks.NotifyAdminsAsync(dbContext, notificationPublisher, link.OrganizationId, link, NotificationTypes.ParentLinkRejected,
            "Empresa matriz rechazada",
            $"Hapag-Lloyd rechazó la asociación con {dto.Parent.Name} como empresa matriz. Motivo: {link.DecisionNotes}",
            cancellationToken);

        return Result<ParentLinkDto>.Success(dto);
    }
}
