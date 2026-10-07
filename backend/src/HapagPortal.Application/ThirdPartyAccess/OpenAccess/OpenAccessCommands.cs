namespace HapagPortal.Application.ThirdPartyAccess.OpenAccess;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Configuración de acceso abierto por número de BL de la organización (M1-17).</summary>
public sealed record GetOpenAccessSettingQuery : IQuery<OpenAccessSettingDto>;

/// <summary>
/// Activa o desactiva el acceso abierto por número de BL y define su único conjunto de permisos
/// (M1-15, M1-17). Nulo = nivel base de M1-11 de quien consulta. Queda en la auditoría (M1-23).
/// </summary>
public sealed record UpdateOpenAccessSettingCommand(
    bool IsEnabled,
    IReadOnlyList<string>? ActionCodes = null,
    bool ResetToBaseLevel = false) : ICommand<OpenAccessSettingDto>;

/// <summary>
/// Asocia de forma permanente a la organización con un BL que consultó por acceso abierto (M1-18):
/// queda en su listado y habilita incorporar sus cargos al carro.
/// </summary>
public sealed record SelfAssociateShipmentCommand(string BlNumber) : ICommand<ShipmentAssociationDto>;

public sealed class UpdateOpenAccessSettingCommandValidator : AbstractValidator<UpdateOpenAccessSettingCommand>
{
    public UpdateOpenAccessSettingCommandValidator()
    {
        RuleFor(x => x.ActionCodes).Must(c => c!.Count <= 100).When(x => x.ActionCodes is not null);
        RuleFor(x => x.ActionCodes).Null().When(x => x.ResetToBaseLevel)
            .WithMessage("Use either ActionCodes or ResetToBaseLevel.");
    }
}

public sealed class SelfAssociateShipmentCommandValidator : AbstractValidator<SelfAssociateShipmentCommand>
{
    public SelfAssociateShipmentCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class GetOpenAccessSettingQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetOpenAccessSettingQuery, OpenAccessSettingDto>
{
    public async Task<Result<OpenAccessSettingDto>> Handle(GetOpenAccessSettingQuery request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<OpenAccessSettingDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var setting = await dbContext.OpenAccessSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ClientId == context.OrganizationId, cancellationToken);

        return Result<OpenAccessSettingDto>.Success(OpenAccessMapper.ToDto(context, setting));
    }
}

public sealed class UpdateOpenAccessSettingCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<UpdateOpenAccessSettingCommand, OpenAccessSettingDto>
{
    public async Task<Result<OpenAccessSettingDto>> Handle(UpdateOpenAccessSettingCommand request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<OpenAccessSettingDto>.Failure(loaded.Error);

        var context = loaded.Value;
        if (!OpenAccessMapper.CanManage(context))
            return Result<OpenAccessSettingDto>.Failure(DomainErrors.OpenAccess.NotAllowed);

        var now = DateTime.UtcNow;
        var setting = await dbContext.OpenAccessSettings
            .FirstOrDefaultAsync(s => s.ClientId == context.OrganizationId, cancellationToken);

        var isNew = setting is null;
        setting ??= new OpenAccessSetting { ClientId = context.OrganizationId };

        var previous = new { setting.IsEnabled, actionCodes = ActionCodeList.ParseNullable(setting.ActionCodes) };

        if (request.ActionCodes is not null || request.ResetToBaseLevel)
        {
            var normalized = GrantRules.NormalizeExplicit(context.Matrix, request.ResetToBaseLevel ? null : request.ActionCodes);
            if (normalized.IsFailure)
                return Result<OpenAccessSettingDto>.Failure(normalized.Error);

            // Quien consulta puede ser cualquiera: el conjunto se valida contra la columna Tercero y contra
            // lo que el titular posee; cada BL lo recorta además a lo que el titular posee en él.
            var grantorActions = context.Matrix.AllowedForRoles(context.OrganizationType, [ShipmentRoleCodes.Customer]);
            var error = GrantRules.CheckGrantable(context.Matrix, OrganizationTypes.Customer, ShipmentRoleCodes.ThirdParty, normalized.Value)
                ?? GrantRules.CheckGrantorLevel(normalized.Value, grantorActions);
            if (error is not null)
                return Result<OpenAccessSettingDto>.Failure(error);

            setting.ActionCodes = normalized.Value is null ? null : ActionCodeList.Format(normalized.Value);
        }

        setting.IsEnabled = request.IsEnabled;
        setting.ChangedAt = now;
        setting.ChangedByUserId = context.Actor.UserId;

        if (isNew)
            dbContext.OpenAccessSettings.Add(setting);

        var current = new { setting.IsEnabled, actionCodes = ActionCodeList.ParseNullable(setting.ActionCodes) };

        if (previous.IsEnabled != setting.IsEnabled)
        {
            AccessAudit.ForOrganization(dbContext,
                setting.IsEnabled ? AccessAuditEvents.OpenAccessEnabled : AccessAuditEvents.OpenAccessDisabled,
                context.Actor, now, context.OrganizationId, details: current);
        }

        if (!Same(previous.actionCodes, current.actionCodes))
        {
            AccessAudit.ForOrganization(dbContext, AccessAuditEvents.OpenAccessPermissionsChanged,
                context.Actor, now, context.OrganizationId, details: new { previous = previous.actionCodes, current = current.actionCodes });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<OpenAccessSettingDto>.Success(OpenAccessMapper.ToDto(context, setting));
    }

    private static bool Same(IReadOnlyList<string>? left, IReadOnlyList<string>? right) =>
        left is null ? right is null : right is not null && left.OrderBy(c => c).SequenceEqual(right.OrderBy(c => c));
}

public sealed class SelfAssociateShipmentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<SelfAssociateShipmentCommand, ShipmentAssociationDto>
{
    public async Task<Result<ShipmentAssociationDto>> Handle(SelfAssociateShipmentCommand request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<ShipmentAssociationDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var blNumber = request.BlNumber.Trim();

        // Solo por el número exacto con acceso abierto activo; sin él, el BL no existe para el usuario.
        var bl = await accessEvaluator.FilterOpenAccess(dbContext.BillsOfLading.AsNoTracking(), context.Scope)
            .FirstOrDefaultAsync(b => b.BLNumber == blNumber, cancellationToken);
        if (bl is null)
            return Result<ShipmentAssociationDto>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(blNumber));

        var already = await dbContext.ShipmentAssociations.AnyAsync(
            a => a.BillOfLadingId == bl.Id && a.ClientId == context.OrganizationId, cancellationToken);
        if (already)
            return Result<ShipmentAssociationDto>.Failure(DomainErrors.ShipmentAssociation.AlreadyExists);

        var permissions = await accessEvaluator.EvaluateAsync(context.Scope, bl, cancellationToken);
        if (!permissions.CanSelfAssociate)
            return Result<ShipmentAssociationDto>.Failure(DomainErrors.OpenAccess.NotAvailable);

        var now = DateTime.UtcNow;
        var association = new ShipmentAssociation
        {
            BillOfLadingId = bl.Id,
            ClientId = context.OrganizationId,
            AssociatedByUserId = context.Membership.User.Id,
            AssociatedAt = now
        };
        dbContext.ShipmentAssociations.Add(association);

        AccessAudit.ForShipment(dbContext, AccessAuditEvents.SelfAssociated, context.Actor, now, bl,
            grantorId: bl.ClientId, granteeId: context.OrganizationId);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ShipmentAssociationDto>.Success(new ShipmentAssociationDto(bl.Id, bl.BLNumber, now));
    }
}

public static class OpenAccessMapper
{
    /// <summary>Activar el acceso abierto es una acción del customer en M1-11 (X para terceros, agencias y transportistas).</summary>
    public static bool CanManage(AccessManagementContext context) =>
        context.Matrix.OrganizationCan(context.OrganizationType, ShipmentActionCodes.EnableOpenAccess, [ShipmentRoleCodes.Customer]);

    public static OpenAccessSettingDto ToDto(AccessManagementContext context, OpenAccessSetting? setting)
    {
        var explicitActions = ActionCodeList.ParseNullable(setting?.ActionCodes);
        var ceiling = context.Matrix.AllowedForRoles(context.OrganizationType, [ShipmentRoleCodes.Customer]);
        var effective = context.Matrix.InformationOnly(
            context.Matrix.AllowedByGrant(OrganizationTypes.Customer, ShipmentRoleCodes.ThirdParty, explicitActions, ceiling));

        return new OpenAccessSettingDto(
            setting?.IsEnabled ?? false,
            explicitActions,
            effective,
            CanManage(context) && context.Scope.CanOperate,
            setting?.ChangedAt);
    }
}
