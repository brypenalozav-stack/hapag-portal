namespace HapagPortal.Application.ThirdPartyAccess.Widenings;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Ampliaciones de visibilidad registradas sobre un BL (M1-16).</summary>
public sealed record GetVisibilityWideningsQuery(string BlNumber, bool IncludeRevoked = false)
    : IQuery<IReadOnlyList<VisibilityWideningDto>>;

/// <summary>
/// Amplía, dato por dato (código de acción), lo que otro rol del mismo BL puede ver (M1-16): solo
/// datos con nivel X (o) para ese rol y que quien amplía posee. Si el dato proviene de un acceso
/// recibido, la ampliación queda ligada a él y cae con su revocación (M1-22).
/// </summary>
public sealed record CreateVisibilityWideningsCommand(string BlNumber, string TargetRole, IReadOnlyList<string> ActionCodes)
    : ICommand<IReadOnlyList<VisibilityWideningDto>>;

/// <summary>Revierte una ampliación otorgada por la propia organización (M1-16, M1-22).</summary>
public sealed record RevokeVisibilityWideningCommand(string BlNumber, Guid Id) : ICommand;

public sealed class GetVisibilityWideningsQueryValidator : AbstractValidator<GetVisibilityWideningsQuery>
{
    public GetVisibilityWideningsQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateVisibilityWideningsCommandValidator : AbstractValidator<CreateVisibilityWideningsCommand>
{
    public CreateVisibilityWideningsCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TargetRole)
            .Must(r => ShipmentRoleCodes.ShipmentParties.Contains(r))
            .WithMessage("TargetRole must be Customer, Shipper or Consignee.");
        RuleFor(x => x.ActionCodes).NotEmpty().Must(c => c.Count <= 50);
        RuleForEach(x => x.ActionCodes).NotEmpty().MaximumLength(100);
    }
}

public sealed class RevokeVisibilityWideningCommandValidator : AbstractValidator<RevokeVisibilityWideningCommand>
{
    public RevokeVisibilityWideningCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetVisibilityWideningsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetVisibilityWideningsQuery, IReadOnlyList<VisibilityWideningDto>>
{
    public async Task<Result<IReadOnlyList<VisibilityWideningDto>>> Handle(
        GetVisibilityWideningsQuery request,
        CancellationToken cancellationToken)
    {
        var loaded = await WideningLoader.LoadAsync(dbContext, currentUserService, accessEvaluator, request.BlNumber, false, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<VisibilityWideningDto>>.Failure(loaded.Error);

        var (context, bl, _) = loaded.Value;
        var query = dbContext.VisibilityWidenings.AsNoTracking().Where(w => w.BillOfLadingId == bl.Id);
        if (!request.IncludeRevoked)
            query = query.Where(w => w.Status == VisibilityWideningStatus.Active);

        var widenings = await query.OrderByDescending(w => w.CreatedAt).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<VisibilityWideningDto>>.Success(
            await WideningLoader.ToDtosAsync(dbContext, widenings, bl, context.OrganizationId, cancellationToken));
    }
}

public sealed class CreateVisibilityWideningsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<CreateVisibilityWideningsCommand, IReadOnlyList<VisibilityWideningDto>>
{
    public async Task<Result<IReadOnlyList<VisibilityWideningDto>>> Handle(
        CreateVisibilityWideningsCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await WideningLoader.LoadAsync(dbContext, currentUserService, accessEvaluator, request.BlNumber, true, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<VisibilityWideningDto>>.Failure(loaded.Error);

        var (context, bl, permissions) = loaded.Value;
        var matrix = context.Matrix;

        if (!permissions.CanExecute(ShipmentActionCodes.ExtendDataVisibility))
            return Result<IReadOnlyList<VisibilityWideningDto>>.Failure(DomainErrors.AccessGrant.NotAllowed);

        var grantorRole = GrantRules.GrantorRoleOf(permissions);
        if (grantorRole == request.TargetRole)
            return Result<IReadOnlyList<VisibilityWideningDto>>.Failure(DomainErrors.VisibilityWidening.InvalidTargetRole);

        var codes = request.ActionCodes.Select(c => c.Trim()).Distinct().ToList();
        var actions = matrix.ShipmentScopedActions
            .Where(a => a.Category == ShipmentActionCategories.Information)
            .ToDictionary(a => a.Code);

        var unknown = codes.Where(c => !actions.ContainsKey(c)).ToList();
        if (unknown.Count > 0)
            return Result<IReadOnlyList<VisibilityWideningDto>>.Failure(DomainErrors.AccessGrant.UnknownActions(unknown));

        // Nunca más de lo que posee quien amplía (M1-16).
        var exceeding = codes.Where(c => !permissions.Can(c)).ToList();
        if (exceeding.Count > 0)
            return Result<IReadOnlyList<VisibilityWideningDto>>.Failure(DomainErrors.AccessGrant.ExceedsGrantorLevel(exceeding));

        // Solo se amplía lo que el rol destino no ve por defecto pero puede recibir: X (o). X no se otorga.
        var notWidenable = codes
            .Where(c => matrix.LevelOf(actions[c], request.TargetRole, null) != AccessLevels.OnGrant)
            .ToList();
        if (notWidenable.Count > 0)
            return Result<IReadOnlyList<VisibilityWideningDto>>.Failure(DomainErrors.VisibilityWidening.NotWidenable(notWidenable));

        var existing = await dbContext.VisibilityWidenings.AsNoTracking()
            .Where(w => w.BillOfLadingId == bl.Id
                && w.GrantorClientId == context.OrganizationId
                && w.TargetRole == request.TargetRole
                && w.Status == VisibilityWideningStatus.Active)
            .Select(w => w.ActionCode)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var created = new List<VisibilityWidening>();

        foreach (var code in codes.Where(c => !existing.Contains(c)))
        {
            var widening = new VisibilityWidening
            {
                BillOfLadingId = bl.Id,
                GrantorClientId = context.OrganizationId,
                GrantorRole = grantorRole,
                TargetRole = request.TargetRole,
                ActionCode = code,
                OriginGrantId = permissions.GrantFor(code)?.GrantId,
                Status = VisibilityWideningStatus.Active,
                GrantedByUserId = context.Actor.UserId
            };
            dbContext.VisibilityWidenings.Add(widening);
            AccessAudit.ForWidening(dbContext, AccessAuditEvents.WideningCreated, context.Actor, now, widening, bl.BLNumber,
                new { targetRole = request.TargetRole, actionCode = code, originGrantId = widening.OriginGrantId });
            created.Add(widening);
        }

        if (created.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<VisibilityWideningDto>>.Success(
            await WideningLoader.ToDtosAsync(dbContext, created, bl, context.OrganizationId, cancellationToken));
    }
}

public sealed class RevokeVisibilityWideningCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<RevokeVisibilityWideningCommand>
{
    public async Task<Result> Handle(RevokeVisibilityWideningCommand request, CancellationToken cancellationToken)
    {
        var loaded = await WideningLoader.LoadAsync(dbContext, currentUserService, accessEvaluator, request.BlNumber, true, cancellationToken);
        if (loaded.IsFailure)
            return Result.Failure(loaded.Error);

        var (context, bl, _) = loaded.Value;
        var widening = await dbContext.VisibilityWidenings.FirstOrDefaultAsync(
            w => w.Id == request.Id
                && w.BillOfLadingId == bl.Id
                && w.GrantorClientId == context.OrganizationId
                && w.Status == VisibilityWideningStatus.Active,
            cancellationToken);

        if (widening is null)
            return Result.Failure(DomainErrors.VisibilityWidening.NotFound(request.Id));

        var now = DateTime.UtcNow;
        AccessGrantCascade.End(widening, context.Actor, now, AccessEndReasons.Manual);
        AccessAudit.ForWidening(dbContext, AccessAuditEvents.WideningRevoked, context.Actor, now, widening, bl.BLNumber);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public static class WideningLoader
{
    /// <summary>Contexto y BL accesible (propio u otorgado); ajeno o inexistente = NotFound.</summary>
    public static async Task<Result<(AccessManagementContext Context, BillOfLading Bl, ShipmentPermissionSet Permissions)>> LoadAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IShipmentAccessEvaluator accessEvaluator,
        string blNumber,
        bool requireOperate,
        CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate, cancellationToken);
        if (loaded.IsFailure)
            return Result<(AccessManagementContext, BillOfLading, ShipmentPermissionSet)>.Failure(loaded.Error);

        var number = blNumber.Trim();
        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), loaded.Value.Scope)
            .FirstOrDefaultAsync(b => b.BLNumber == number, cancellationToken);
        if (bl is null)
            return Result<(AccessManagementContext, BillOfLading, ShipmentPermissionSet)>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(number));

        var permissions = await accessEvaluator.EvaluateAsync(loaded.Value.Scope, bl, cancellationToken);
        if (!permissions.Can(ShipmentActionCodes.ViewShipment))
            return Result<(AccessManagementContext, BillOfLading, ShipmentPermissionSet)>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(number));

        return Result<(AccessManagementContext, BillOfLading, ShipmentPermissionSet)>.Success((loaded.Value, bl, permissions));
    }

    public static async Task<IReadOnlyList<VisibilityWideningDto>> ToDtosAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<VisibilityWidening> widenings,
        BillOfLading bl,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organizations = await AccessGrantMapper.LoadOrganizationsAsync(
            dbContext, widenings.Select(w => w.GrantorClientId), cancellationToken);

        return widenings.Select(w => new VisibilityWideningDto(
            w.Id,
            bl.Id,
            bl.BLNumber,
            organizations.TryGetValue(w.GrantorClientId, out var grantor)
                ? AccessGrantMapper.ToRef(grantor)
                : new OrganizationRefDto(w.GrantorClientId, string.Empty, string.Empty, string.Empty),
            w.GrantorRole,
            w.TargetRole,
            w.ActionCode,
            w.OriginGrantId,
            w.Status,
            w.CreatedAt,
            w.EndedAt,
            w.EndReason,
            w.GrantorClientId == organizationId && w.Status == VisibilityWideningStatus.Active)).ToList();
    }
}
