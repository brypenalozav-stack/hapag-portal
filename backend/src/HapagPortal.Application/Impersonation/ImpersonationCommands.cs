namespace HapagPortal.Application.Impersonation;

using System.Text.Json;
using FluentValidation;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record ImpersonationUserDto(Guid UserId, string Email, string? FullName);

public sealed record ImpersonationOrganizationDto(Guid Id, string Name, string? TaxId, string? OrganizationType);

/// <summary>
/// Sesión de «Vista como cliente» (M8-08): actor interno, cliente impersonado, organización, motivo, inicio, término,
/// duración y cantidad de solicitudes (permitidas y bloqueadas). <see cref="ReadOnly"/> = sin escrituras permitidas.
/// </summary>
public sealed record ImpersonationSessionDto(
    Guid Id,
    ImpersonationUserDto Actor,
    ImpersonationUserDto Subject,
    ImpersonationOrganizationDto Organization,
    string Reason,
    string Status,
    DateTime StartedAt,
    DateTime ExpiresAt,
    DateTime? EndedAt,
    string? EndReason,
    int? DurationSeconds,
    int RequestCount,
    int BlockedCount,
    bool ReadOnly,
    IReadOnlyList<string> AllowedActions);

/// <summary>Inicio de la sesión: token del cliente marcado como impersonación (sin refresh token) y la sesión.</summary>
public sealed record ImpersonationStartedDto(AuthResponseDto Auth, ImpersonationSessionDto Session);

/// <summary>Usuario de una organización cliente que puede verse como cliente.</summary>
public sealed record ImpersonationTargetDto(
    Guid UserId,
    string Email,
    string FullName,
    string? Profile,
    bool IsActive,
    string MembershipStatus,
    bool Eligible,
    DateTime? LastLoginAt);

/// <summary>Solicitud hecha durante una sesión, registrada con la identidad del actor (M8-08, NF-14).</summary>
public sealed record ImpersonationRequestDto(Guid Id, string Action, DateTime Timestamp, string? Method, string? Path, int? StatusCode);

/// <summary>Inicia una «Vista como cliente» del usuario indicado de la organización indicada.</summary>
public sealed record StartImpersonationCommand(Guid OrganizationId, Guid UserId, string Reason) : ICommand<ImpersonationStartedDto>;

/// <summary>Termina la sesión actual (con el token de impersonación).</summary>
public sealed record EndCurrentImpersonationCommand : ICommand<ImpersonationSessionDto>;

/// <summary>Termina una sesión activa desde el área de administración.</summary>
public sealed record EndImpersonationSessionCommand(Guid Id) : ICommand<ImpersonationSessionDto>;

/// <summary>Sesión del token actual (la interfaz muestra el aviso de impersonación).</summary>
public sealed record GetCurrentImpersonationQuery : IQuery<ImpersonationSessionDto>;

public sealed record GetImpersonationSessionsQuery(
    Guid? ActorUserId = null,
    Guid? OrganizationId = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ImpersonationSessionDto>>;

public sealed record GetImpersonationRequestsQuery(Guid Id, int Page = 1, int PageSize = 50)
    : IQuery<PagedResult<ImpersonationRequestDto>>;

/// <summary>Usuarios de una organización cliente, indicando cuáles pueden impersonarse.</summary>
public sealed record GetImpersonationTargetsQuery(Guid OrganizationId) : IQuery<IReadOnlyList<ImpersonationTargetDto>>;

public sealed class StartImpersonationCommandValidator : AbstractValidator<StartImpersonationCommand>
{
    public StartImpersonationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Reglas de la «Vista como cliente» (M8-08): quién puede iniciarla, a quién y qué escrituras se permiten.</summary>
public static class ImpersonationPolicy
{
    /// <summary>Escrituras siempre permitidas: terminar la sesión (también al cerrar sesión).</summary>
    public static readonly string[] AlwaysAllowedWrites = ["POST /api/v1/impersonation/end", "POST /api/v1/auth/logout"];

    /// <summary>Roles internos: un usuario con alguno de ellos nunca se impersona.</summary>
    public static readonly string[] InternalRoles =
        [.. RoleCodes.InternalAdministrators, RoleCodes.Coordinador, RoleCodes.Supervisor, RoleCodes.ExternalApi];

    public static bool IsRead(string method) =>
        method.Equals("GET", StringComparison.OrdinalIgnoreCase)
        || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
        || method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase);

    /// <summary>Lectura, una escritura siempre permitida o una de las configuradas (prefijo de ruta por segmentos).</summary>
    public static bool IsAllowed(string method, string path, IEnumerable<string> configured)
    {
        if (IsRead(method))
            return true;

        return AlwaysAllowedWrites.Concat(configured).Any(rule => Matches(rule, method, path));
    }

    private static bool Matches(string rule, string method, string path)
    {
        var parts = rule.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !parts[0].Equals(method, StringComparison.OrdinalIgnoreCase))
            return false;

        var prefix = parts[1].TrimEnd('/');
        var normalized = path.TrimEnd('/');
        return normalized.Equals(prefix, StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Usuario interno: de la organización Hapag-Lloyd o sin organización.</summary>
    public static async Task<bool> IsInternalUserAsync(IApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
            return false;
        if (user.ClientId is null)
            return true;

        var type = await dbContext.Clients.AsNoTracking()
            .Where(c => c.Id == user.ClientId.Value)
            .Select(c => c.OrganizationType)
            .FirstOrDefaultAsync(cancellationToken);
        return type == OrganizationTypes.Internal;
    }
}

internal static class ImpersonationViews
{
    public static async Task<ImpersonationSessionDto> ToDtoAsync(
        IApplicationDbContext dbContext,
        ImpersonationSession session,
        ImpersonationSettings settings,
        CancellationToken cancellationToken)
    {
        var people = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == session.ActorUserId || u.Id == session.SubjectUserId)
            .ToListAsync(cancellationToken);
        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == session.OrganizationId, cancellationToken);
        return ToDto(session, people, organization, settings);
    }

    public static ImpersonationSessionDto ToDto(
        ImpersonationSession session,
        IReadOnlyCollection<User> people,
        Client? organization,
        ImpersonationSettings settings)
    {
        var actor = people.FirstOrDefault(u => u.Id == session.ActorUserId);
        var subject = people.FirstOrDefault(u => u.Id == session.SubjectUserId);
        return new ImpersonationSessionDto(
            session.Id,
            new ImpersonationUserDto(session.ActorUserId, session.ActorEmail, actor is null ? null : OrganizationMapper.FullNameOf(actor)),
            new ImpersonationUserDto(session.SubjectUserId, session.SubjectEmail, subject is null ? null : OrganizationMapper.FullNameOf(subject)),
            new ImpersonationOrganizationDto(session.OrganizationId, session.OrganizationName, organization?.TaxId, organization?.OrganizationType),
            session.Reason,
            session.Status,
            session.StartedAt,
            session.ExpiresAt,
            session.EndedAt,
            session.EndReason,
            session.DurationSeconds,
            session.RequestCount,
            session.BlockedCount,
            settings.AllowedActions.Length == 0,
            settings.AllowedActions);
    }
}

/// <summary>Término de una sesión con su duración y su registro en <c>AuditLogs</c> (M8-08). No guarda.</summary>
public static class ImpersonationSessions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void End(IApplicationDbContext dbContext, ImpersonationSession session, string reason, DateTime now)
    {
        if (session.Status != ImpersonationStatus.Active)
            return;

        var endedAt = reason == ImpersonationEndReasons.Expired && session.ExpiresAt < now ? session.ExpiresAt : now;
        session.Status = reason == ImpersonationEndReasons.Expired ? ImpersonationStatus.Expired : ImpersonationStatus.Ended;
        session.EndedAt = endedAt;
        session.EndReason = reason;
        session.DurationSeconds = (int)Math.Max(0, (endedAt - session.StartedAt).TotalSeconds);

        Audit(dbContext, session, ImpersonationAuditActions.Ended, now, new
        {
            reason,
            durationSeconds = session.DurationSeconds,
            session.RequestCount,
            session.BlockedCount
        });
    }

    public static void Audit(IApplicationDbContext dbContext, ImpersonationSession session, string action, DateTime now, object details) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            EntityName = ImpersonationAuditActions.EntityName,
            EntityId = session.Id.ToString(),
            Action = action,
            NewValues = JsonSerializer.Serialize(new
            {
                actorUserId = session.ActorUserId,
                actorEmail = session.ActorEmail,
                subjectUserId = session.SubjectUserId,
                subjectEmail = session.SubjectEmail,
                organizationId = session.OrganizationId,
                organizationName = session.OrganizationName,
                details
            }, JsonOptions),
            UserId = session.ActorUserId.ToString(),
            Timestamp = now
        });
}

public sealed class StartImpersonationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPermissionResolver permissionResolver,
    IJwtTokenService jwtTokenService,
    ImpersonationSettings settings)
    : ICommandHandler<StartImpersonationCommand, ImpersonationStartedDto>
{
    public async Task<Result<ImpersonationStartedDto>> Handle(StartImpersonationCommand request, CancellationToken cancellationToken)
    {
        // Sin anidamiento: con un token de impersonación no se inicia otra (ni con un permiso heredado del cliente).
        if (currentUserService.IsImpersonating)
            return Result<ImpersonationStartedDto>.Failure(DomainErrors.Impersonation.Nested);

        if (currentUserService.UserId is not { } actorId)
            return Result<ImpersonationStartedDto>.Failure(Error.Unauthorized);

        if (!currentUserService.HasPermission(AdministrationPermissions.UseImpersonation)
            || !await ImpersonationPolicy.IsInternalUserAsync(dbContext, actorId, cancellationToken))
            return Result<ImpersonationStartedDto>.Failure(DomainErrors.Impersonation.NotInternalActor);

        var actor = await dbContext.Users.AsNoTracking().FirstAsync(u => u.Id == actorId, cancellationToken);

        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.OrganizationId, cancellationToken);
        if (organization is null)
            return Result<ImpersonationStartedDto>.Failure(DomainErrors.Organization.NotFound(request.OrganizationId));
        if (organization.OrganizationType == OrganizationTypes.Internal || !organization.IsActive)
            return Result<ImpersonationStartedDto>.Failure(DomainErrors.Impersonation.TargetNotAllowed);

        var subject = await dbContext.Users.Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (subject is null || subject.ClientId != organization.Id)
            return Result<ImpersonationStartedDto>.Failure(DomainErrors.Impersonation.TargetNotFound);

        var roles = await dbContext.UserRoles.AsNoTracking()
            .Where(r => r.UserId == subject.Id)
            .Select(r => r.RoleName)
            .ToListAsync(cancellationToken);

        if (subject.Id == actorId
            || !subject.IsActive
            || subject.MembershipStatus != MembershipStatus.Active
            || roles.Any(r => ImpersonationPolicy.InternalRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            return Result<ImpersonationStartedDto>.Failure(DomainErrors.Impersonation.TargetNotAllowed);

        var now = DateTime.UtcNow;

        // Una sola sesión activa por actor: la anterior termina al iniciar otra.
        var previous = await dbContext.ImpersonationSessions
            .Where(s => s.ActorUserId == actorId && s.Status == ImpersonationStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var open in previous)
            ImpersonationSessions.End(dbContext, open, open.ExpiresAt <= now ? ImpersonationEndReasons.Expired : ImpersonationEndReasons.Replaced, now);

        var session = new ImpersonationSession
        {
            ActorUserId = actorId,
            ActorEmail = actor.Email,
            SubjectUserId = subject.Id,
            SubjectEmail = subject.Email,
            OrganizationId = organization.Id,
            OrganizationName = organization.Name,
            Reason = request.Reason.Trim(),
            Status = ImpersonationStatus.Active,
            StartedAt = now,
            ExpiresAt = now.Add(settings.SessionLength)
        };
        dbContext.ImpersonationSessions.Add(session);
        ImpersonationSessions.Audit(dbContext, session, ImpersonationAuditActions.Started, now, new
        {
            reason = session.Reason,
            expiresAt = session.ExpiresAt,
            readOnly = settings.AllowedActions.Length == 0,
            allowedActions = settings.AllowedActions
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        // Mismos roles y permisos del cliente (M1-11); las credenciales y el refresh token del cliente no se tocan.
        var permissions = await permissionResolver.ResolveAsync(roles, cancellationToken) ?? [];
        var token = jwtTokenService.GenerateImpersonationToken(subject, roles, permissions.ToList(), session);

        var userDto = new ClientResponseDto(
            subject.Id,
            organization.Name,
            subject.Email,
            organization.TaxId,
            organization.Phone,
            subject.Country,
            organization.ClientType is "Agent" or "CustomsAgent" ? "AGENT" : "CLIENT",
            "USER",
            subject.IsActive,
            subject.CreatedAt);
        var summary = OrganizationMapper.ToSummary(organization, subject, roles, permissions.Contains(AccessPermissions.OperateShipments));
        var expiresIn = (int)Math.Ceiling((session.ExpiresAt - now).TotalMinutes);

        return Result<ImpersonationStartedDto>.Success(new ImpersonationStartedDto(
            new AuthResponseDto(token, expiresIn, userDto, null, summary),
            ImpersonationViews.ToDto(session, [actor, subject], organization, settings)));
    }
}

public sealed class EndCurrentImpersonationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ImpersonationSettings settings)
    : ICommandHandler<EndCurrentImpersonationCommand, ImpersonationSessionDto>
{
    public async Task<Result<ImpersonationSessionDto>> Handle(EndCurrentImpersonationCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.ImpersonationSessionId is not { } sessionId)
            return Result<ImpersonationSessionDto>.Failure(DomainErrors.Impersonation.NotImpersonating);

        var session = await dbContext.ImpersonationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
            return Result<ImpersonationSessionDto>.Failure(DomainErrors.Impersonation.NotFound(sessionId));

        var now = DateTime.UtcNow;
        ImpersonationSessions.End(dbContext, session, session.ExpiresAt <= now ? ImpersonationEndReasons.Expired : ImpersonationEndReasons.Manual, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ImpersonationSessionDto>.Success(await ImpersonationViews.ToDtoAsync(dbContext, session, settings, cancellationToken));
    }
}

public sealed class EndImpersonationSessionCommandHandler(
    IApplicationDbContext dbContext,
    ImpersonationSettings settings)
    : ICommandHandler<EndImpersonationSessionCommand, ImpersonationSessionDto>
{
    public async Task<Result<ImpersonationSessionDto>> Handle(EndImpersonationSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await dbContext.ImpersonationSessions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (session is null)
            return Result<ImpersonationSessionDto>.Failure(DomainErrors.Impersonation.NotFound(request.Id));
        if (session.Status != ImpersonationStatus.Active)
            return Result<ImpersonationSessionDto>.Failure(DomainErrors.Impersonation.NotActive);

        var now = DateTime.UtcNow;
        ImpersonationSessions.End(dbContext, session, session.ExpiresAt <= now ? ImpersonationEndReasons.Expired : ImpersonationEndReasons.Admin, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ImpersonationSessionDto>.Success(await ImpersonationViews.ToDtoAsync(dbContext, session, settings, cancellationToken));
    }
}

public sealed class GetCurrentImpersonationQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ImpersonationSettings settings)
    : IQueryHandler<GetCurrentImpersonationQuery, ImpersonationSessionDto>
{
    public async Task<Result<ImpersonationSessionDto>> Handle(GetCurrentImpersonationQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.ImpersonationSessionId is not { } sessionId)
            return Result<ImpersonationSessionDto>.Failure(DomainErrors.Impersonation.NotImpersonating);

        var session = await dbContext.ImpersonationSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        return session is null
            ? Result<ImpersonationSessionDto>.Failure(DomainErrors.Impersonation.NotFound(sessionId))
            : Result<ImpersonationSessionDto>.Success(await ImpersonationViews.ToDtoAsync(dbContext, session, settings, cancellationToken));
    }
}

public sealed class GetImpersonationSessionsQueryHandler(
    IApplicationDbContext dbContext,
    ImpersonationSettings settings)
    : IQueryHandler<GetImpersonationSessionsQuery, PagedResult<ImpersonationSessionDto>>
{
    public async Task<Result<PagedResult<ImpersonationSessionDto>>> Handle(GetImpersonationSessionsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = dbContext.ImpersonationSessions.AsNoTracking();
        if (request.ActorUserId is { } actorId)
            query = query.Where(s => s.ActorUserId == actorId);
        if (request.OrganizationId is { } organizationId)
            query = query.Where(s => s.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(s => s.Status == request.Status);
        if (request.From is { } from)
            query = query.Where(s => s.StartedAt >= from);
        if (request.To is { } to)
            query = query.Where(s => s.StartedAt <= to);

        var total = await query.CountAsync(cancellationToken);
        var sessions = await query.OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = sessions.SelectMany(s => new[] { s.ActorUserId, s.SubjectUserId }).Distinct().ToList();
        var people = await dbContext.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);
        var organizationIds = sessions.Select(s => s.OrganizationId).Distinct().ToList();
        var organizations = await dbContext.Clients.AsNoTracking().Where(c => organizationIds.Contains(c.Id)).ToListAsync(cancellationToken);

        var items = sessions
            .Select(s => ImpersonationViews.ToDto(s, people, organizations.FirstOrDefault(o => o.Id == s.OrganizationId), settings))
            .ToList();
        return Result<PagedResult<ImpersonationSessionDto>>.Success(new PagedResult<ImpersonationSessionDto>(items, total, page, pageSize));
    }
}

public sealed class GetImpersonationRequestsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetImpersonationRequestsQuery, PagedResult<ImpersonationRequestDto>>
{
    public async Task<Result<PagedResult<ImpersonationRequestDto>>> Handle(GetImpersonationRequestsQuery request, CancellationToken cancellationToken)
    {
        if (!await dbContext.ImpersonationSessions.AnyAsync(s => s.Id == request.Id, cancellationToken))
            return Result<PagedResult<ImpersonationRequestDto>>.Failure(DomainErrors.Impersonation.NotFound(request.Id));

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;
        var entityId = request.Id.ToString();

        var query = dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == ImpersonationAuditActions.EntityName && a.EntityId == entityId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(a =>
        {
            string? method = null, path = null;
            int? status = null;
            if (!string.IsNullOrWhiteSpace(a.NewValues))
            {
                using var document = JsonDocument.Parse(a.NewValues);
                if (document.RootElement.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Object)
                {
                    method = details.TryGetProperty("method", out var m) ? m.GetString() : null;
                    path = details.TryGetProperty("path", out var p) ? p.GetString() : null;
                    status = details.TryGetProperty("statusCode", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : null;
                }
            }

            return new ImpersonationRequestDto(a.Id, a.Action, a.Timestamp, method, path, status);
        }).ToList();

        return Result<PagedResult<ImpersonationRequestDto>>.Success(new PagedResult<ImpersonationRequestDto>(items, total, page, pageSize));
    }
}

public sealed class GetImpersonationTargetsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetImpersonationTargetsQuery, IReadOnlyList<ImpersonationTargetDto>>
{
    public async Task<Result<IReadOnlyList<ImpersonationTargetDto>>> Handle(GetImpersonationTargetsQuery request, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.OrganizationId, cancellationToken);
        if (organization is null)
            return Result<IReadOnlyList<ImpersonationTargetDto>>.Failure(DomainErrors.Organization.NotFound(request.OrganizationId));
        if (organization.OrganizationType == OrganizationTypes.Internal)
            return Result<IReadOnlyList<ImpersonationTargetDto>>.Failure(DomainErrors.Impersonation.TargetNotAllowed);

        var users = await dbContext.Users.AsNoTracking()
            .Where(u => u.ClientId == organization.Id)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);
        var ids = users.Select(u => u.Id).ToList();
        var roles = await dbContext.UserRoles.AsNoTracking()
            .Where(r => ids.Contains(r.UserId))
            .Select(r => new { r.UserId, r.RoleName })
            .ToListAsync(cancellationToken);

        IReadOnlyList<ImpersonationTargetDto> items = users.Select(u =>
        {
            var userRoles = roles.Where(r => r.UserId == u.Id).Select(r => r.RoleName).ToList();
            var eligible = organization.IsActive && u.IsActive && u.MembershipStatus == MembershipStatus.Active
                && !userRoles.Any(r => ImpersonationPolicy.InternalRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            return new ImpersonationTargetDto(u.Id, u.Email, OrganizationMapper.FullNameOf(u), OrganizationMapper.ProfileOf(userRoles),
                u.IsActive, u.MembershipStatus, eligible, u.LastLoginAt);
        }).ToList();

        return Result<IReadOnlyList<ImpersonationTargetDto>>.Success(items);
    }
}
