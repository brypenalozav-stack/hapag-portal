namespace HapagPortal.Application.ThirdPartyAccess.Defaults;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Terceros por defecto de la organización (M1-13).</summary>
public sealed record GetDefaultGranteesQuery : IQuery<IReadOnlyList<DefaultGranteeDto>>;

/// <summary>
/// Agrega un tercero por defecto: recibirá acceso automáticamente sobre los BL y bookings nuevos, con
/// los permisos (nulo = nivel base de M1-11) y la duración indicados (M1-13, M1-15).
/// </summary>
public sealed record CreateDefaultGranteeCommand(
    Guid GranteeOrganizationId,
    IReadOnlyList<string>? ActionCodes = null,
    int? DurationDays = null) : ICommand<DefaultGranteeDto>;

/// <summary>Cambia permisos o duración de un tercero por defecto; no altera accesos ya otorgados.</summary>
public sealed record UpdateDefaultGranteeCommand(
    Guid Id,
    IReadOnlyList<string>? ActionCodes = null,
    int? DurationDays = null,
    bool ResetToBaseLevel = false) : ICommand<DefaultGranteeDto>;

/// <summary>Quita un tercero por defecto; los accesos ya otorgados se mantienen.</summary>
public sealed record RemoveDefaultGranteeCommand(Guid Id) : ICommand;

public sealed class CreateDefaultGranteeCommandValidator : AbstractValidator<CreateDefaultGranteeCommand>
{
    public CreateDefaultGranteeCommandValidator()
    {
        RuleFor(x => x.GranteeOrganizationId).NotEmpty();
        RuleFor(x => x.DurationDays).InclusiveBetween(1, GrantRules.MaxDurationDays).When(x => x.DurationDays is not null);
        RuleFor(x => x.ActionCodes).Must(c => c!.Count <= 100).When(x => x.ActionCodes is not null);
    }
}

public sealed class UpdateDefaultGranteeCommandValidator : AbstractValidator<UpdateDefaultGranteeCommand>
{
    public UpdateDefaultGranteeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DurationDays).InclusiveBetween(1, GrantRules.MaxDurationDays).When(x => x.DurationDays is not null);
        RuleFor(x => x.ActionCodes).Null().When(x => x.ResetToBaseLevel)
            .WithMessage("Use either ActionCodes or ResetToBaseLevel.");
    }
}

public sealed class RemoveDefaultGranteeCommandValidator : AbstractValidator<RemoveDefaultGranteeCommand>
{
    public RemoveDefaultGranteeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetDefaultGranteesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetDefaultGranteesQuery, IReadOnlyList<DefaultGranteeDto>>
{
    public async Task<Result<IReadOnlyList<DefaultGranteeDto>>> Handle(GetDefaultGranteesQuery request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<DefaultGranteeDto>>.Failure(loaded.Error);

        var organizationId = loaded.Value.OrganizationId;
        var defaults = await dbContext.DefaultGrantees.AsNoTracking()
            .Where(d => d.GrantorClientId == organizationId && d.IsActive)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var organizations = await AccessGrantMapper.LoadOrganizationsAsync(
            dbContext, defaults.Select(d => d.GranteeClientId), cancellationToken);

        return Result<IReadOnlyList<DefaultGranteeDto>>.Success(
            defaults.Where(d => organizations.ContainsKey(d.GranteeClientId))
                .Select(d => DefaultGranteeMapper.ToDto(d, organizations[d.GranteeClientId]))
                .ToList());
    }
}

public sealed class CreateDefaultGranteeCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<CreateDefaultGranteeCommand, DefaultGranteeDto>
{
    public async Task<Result<DefaultGranteeDto>> Handle(CreateDefaultGranteeCommand request, CancellationToken cancellationToken)
    {
        var loaded = await DefaultGranteeMapper.LoadContextAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result<DefaultGranteeDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var grantee = await dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.GranteeOrganizationId, cancellationToken);
        var granteeError = GrantRules.CheckGrantee(grantee, request.GranteeOrganizationId, context.OrganizationId);
        if (granteeError is not null)
            return Result<DefaultGranteeDto>.Failure(granteeError);

        var duplicate = await dbContext.DefaultGrantees.AnyAsync(
            d => d.GrantorClientId == context.OrganizationId && d.GranteeClientId == grantee!.Id && d.IsActive,
            cancellationToken);
        if (duplicate)
            return Result<DefaultGranteeDto>.Failure(DomainErrors.DefaultGrantee.AlreadyExists);

        var actions = DefaultGranteeMapper.Validate(context, grantee!, request.ActionCodes);
        if (actions.IsFailure)
            return Result<DefaultGranteeDto>.Failure(actions.Error);

        var configured = new DefaultGrantee
        {
            GrantorClientId = context.OrganizationId,
            GranteeClientId = grantee!.Id,
            ActionCodes = actions.Value is null ? null : ActionCodeList.Format(actions.Value),
            DurationDays = request.DurationDays,
            IsActive = true
        };
        dbContext.DefaultGrantees.Add(configured);

        AccessAudit.ForOrganization(dbContext, AccessAuditEvents.DefaultGranteeAdded, context.Actor, DateTime.UtcNow,
            context.OrganizationId, grantee.Id, new { defaultGranteeId = configured.Id, actionCodes = actions.Value, request.DurationDays });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DefaultGranteeDto>.Success(DefaultGranteeMapper.ToDto(configured, grantee));
    }
}

public sealed class UpdateDefaultGranteeCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<UpdateDefaultGranteeCommand, DefaultGranteeDto>
{
    public async Task<Result<DefaultGranteeDto>> Handle(UpdateDefaultGranteeCommand request, CancellationToken cancellationToken)
    {
        var loaded = await DefaultGranteeMapper.LoadContextAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result<DefaultGranteeDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var configured = await dbContext.DefaultGrantees.FirstOrDefaultAsync(
            d => d.Id == request.Id && d.GrantorClientId == context.OrganizationId && d.IsActive, cancellationToken);
        if (configured is null)
            return Result<DefaultGranteeDto>.Failure(DomainErrors.DefaultGrantee.NotFound(request.Id));

        var grantee = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == configured.GranteeClientId, cancellationToken);

        var previous = new { actionCodes = ActionCodeList.ParseNullable(configured.ActionCodes), configured.DurationDays };

        if (request.ActionCodes is not null || request.ResetToBaseLevel)
        {
            var actions = DefaultGranteeMapper.Validate(context, grantee, request.ResetToBaseLevel ? null : request.ActionCodes);
            if (actions.IsFailure)
                return Result<DefaultGranteeDto>.Failure(actions.Error);
            configured.ActionCodes = actions.Value is null ? null : ActionCodeList.Format(actions.Value);
        }

        configured.DurationDays = request.DurationDays;

        // M1-13: cambiar el valor por defecto no toca los accesos ya otorgados.
        AccessAudit.ForOrganization(dbContext, AccessAuditEvents.DefaultGranteeUpdated, context.Actor, DateTime.UtcNow,
            context.OrganizationId, grantee.Id, new
            {
                defaultGranteeId = configured.Id,
                previous,
                current = new { actionCodes = ActionCodeList.ParseNullable(configured.ActionCodes), configured.DurationDays }
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DefaultGranteeDto>.Success(DefaultGranteeMapper.ToDto(configured, grantee));
    }
}

public sealed class RemoveDefaultGranteeCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<RemoveDefaultGranteeCommand>
{
    public async Task<Result> Handle(RemoveDefaultGranteeCommand request, CancellationToken cancellationToken)
    {
        var loaded = await DefaultGranteeMapper.LoadContextAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result.Failure(loaded.Error);

        var context = loaded.Value;
        var configured = await dbContext.DefaultGrantees.FirstOrDefaultAsync(
            d => d.Id == request.Id && d.GrantorClientId == context.OrganizationId && d.IsActive, cancellationToken);
        if (configured is null)
            return Result.Failure(DomainErrors.DefaultGrantee.NotFound(request.Id));

        configured.IsActive = false;

        AccessAudit.ForOrganization(dbContext, AccessAuditEvents.DefaultGranteeRemoved, context.Actor, DateTime.UtcNow,
            context.OrganizationId, configured.GranteeClientId, new { defaultGranteeId = configured.Id });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public static class DefaultGranteeMapper
{
    public static DefaultGranteeDto ToDto(DefaultGrantee configured, Client grantee) =>
        new(
            configured.Id,
            AccessGrantMapper.ToRef(grantee),
            ActionCodeList.ParseNullable(configured.ActionCodes),
            configured.DurationDays,
            configured.CreatedAt,
            configured.ModifiedAt);

    /// <summary>Configurar terceros por defecto es una acción de la organización en M1-11 (X para agencias y transportistas).</summary>
    public static async Task<Result<AccessManagementContext>> LoadContextAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IShipmentAccessEvaluator accessEvaluator,
        CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return loaded;

        return loaded.Value.Matrix.OrganizationCan(loaded.Value.OrganizationType, ShipmentActionCodes.ConfigureDefaultAgents)
            ? loaded
            : Result<AccessManagementContext>.Failure(DomainErrors.AccessGrant.NotAllowed);
    }

    /// <summary>
    /// Permisos de un tercero por defecto: acciones que el tercero puede recibir y que la organización
    /// posee como parte de un embarque. Al aplicarse a cada BL se recortan además a lo que posee en él.
    /// </summary>
    public static Result<IReadOnlyList<string>?> Validate(
        AccessManagementContext context,
        Client grantee,
        IReadOnlyList<string>? actionCodes)
    {
        var normalized = GrantRules.NormalizeExplicit(context.Matrix, actionCodes);
        if (normalized.IsFailure)
            return normalized;

        var grantorActions = context.Matrix.AllowedForRoles(context.OrganizationType, ShipmentRoleCodes.ShipmentParties);
        var error = GrantRules.CheckGrantable(context.Matrix, grantee.OrganizationType, ShipmentRoleCodes.ThirdParty, normalized.Value)
            ?? GrantRules.CheckGrantorLevel(normalized.Value, grantorActions);

        return error is null ? normalized : Result<IReadOnlyList<string>?>.Failure(error);
    }
}
