namespace HapagPortal.Application.Common.Access;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Evaluación de accesos por embarque en el servidor (M1-11, M8-06, NF-05). Sin caché entre
/// solicitudes: un cambio de matriz, de perfil o de estado de la organización aplica de inmediato.
/// </summary>
public sealed class ShipmentAccessEvaluator(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IShipmentAccessEvaluator
{
    private AccessScope? _scope;
    private List<ShipmentAction>? _actions;
    private List<ShipmentAccessRule>? _rules;

    public async Task<AccessScope> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        if (_scope is not null)
            return _scope;

        var userId = currentUserService.UserId;
        if (userId is null)
            return _scope = AccessScope.None;

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user is null || !user.IsActive || user.MembershipStatus != MembershipStatus.Active)
            return _scope = AccessScope.None;

        var organization = user.ClientId is null
            ? null
            : await dbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == user.ClientId.Value, cancellationToken);

        // M8-06: la visibilidad total es solo para usuarios internos, aunque un cliente tuviera el permiso.
        var isInternal = organization is null || organization.OrganizationType == OrganizationTypes.Internal;
        if (isInternal && currentUserService.HasPermission(AccessPermissions.ViewAllShipments))
        {
            return _scope = new AccessScope(
                userId, organization?.Id, OrganizationTypes.Internal, IsAdmin: true, IsOperational: true, CanOperate: true);
        }

        if (organization is null)
            return _scope = AccessScope.None with { UserId = userId };

        var isOperational = organization.IsActive && organization.RegistrationStatus == OrganizationStatus.Approved;

        return _scope = new AccessScope(
            userId,
            organization.Id,
            organization.OrganizationType,
            IsAdmin: false,
            IsOperational: isOperational,
            CanOperate: isOperational && currentUserService.HasPermission(AccessPermissions.OperateShipments));
    }

    public IQueryable<BillOfLading> FilterAccessible(IQueryable<BillOfLading> source, AccessScope scope)
    {
        if (scope.IsAdmin)
            return source;

        if (!scope.IsOperational || scope.OrganizationId is null)
            return source.Where(_ => false);

        var organizationId = scope.OrganizationId.Value;
        var shipmentRoles = dbContext.ShipmentRoles;

        // Embarques propios: titular heredado del BL o rol registrado (Customer/Shipper/Consignee).
        // Ola B suma aquí los accesos otorgados y autoasociados vigentes.
        return source.Where(b =>
            b.ClientId == organizationId ||
            shipmentRoles.Any(r => r.BillOfLadingId == b.Id && r.ClientId == organizationId));
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRolesAsync(
        AccessScope scope,
        IReadOnlyCollection<BillOfLading> billsOfLading,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, IReadOnlyList<string>>();

        if (scope.OrganizationId is null || billsOfLading.Count == 0)
        {
            foreach (var bl in billsOfLading)
                result[bl.Id] = [];
            return result;
        }

        var organizationId = scope.OrganizationId.Value;
        var ids = billsOfLading.Select(b => b.Id).ToList();

        var rows = await dbContext.ShipmentRoles
            .AsNoTracking()
            .Where(r => r.ClientId == organizationId && ids.Contains(r.BillOfLadingId))
            .Select(r => new { r.BillOfLadingId, r.Role })
            .ToListAsync(cancellationToken);

        foreach (var bl in billsOfLading)
        {
            var roles = rows
                .Where(r => r.BillOfLadingId == bl.Id)
                .Select(r => r.Role)
                .ToHashSet();

            if (bl.ClientId == organizationId)
                roles.Add(ShipmentRoleCodes.Customer);

            result[bl.Id] = ShipmentRoleCodes.MatrixColumns.Where(roles.Contains).ToList();
        }

        return result;
    }

    public async Task<ShipmentPermissionSet> EvaluateAsync(
        AccessScope scope,
        BillOfLading billOfLading,
        CancellationToken cancellationToken = default)
    {
        if (!scope.IsOperational)
            return ShipmentPermissionSet.None;

        var roles = (await GetRolesAsync(scope, [billOfLading], cancellationToken))[billOfLading.Id];
        var (actions, rules) = await LoadMatrixAsync(cancellationToken);

        if (scope.IsAdmin)
        {
            return new ShipmentPermissionSet(
                roles, actions.Where(a => a.IsActive).Select(a => a.Code).ToList(), IsAdmin: true, CanOperate: true);
        }

        if (roles.Count == 0)
            return ShipmentPermissionSet.None;

        var columns = AccessLevelResolver.MatrixColumnsFor(scope.OrganizationType, roles);

        // Unión de roles: basta con que una columna permita la acción. Sin otorgamientos todavía (Ola B),
        // X (o) queda denegado.
        var allowed = actions
            .Where(a => a.IsActive && columns.Any(column =>
                AccessLevelResolver.IsAllowed(
                    AccessLevelResolver.ResolveLevel(rules, a.Id, column, scope.OrganizationType))))
            .Select(a => a.Code)
            .ToList();

        return new ShipmentPermissionSet(columns, allowed, IsAdmin: false, scope.CanOperate);
    }

    private async Task<(List<ShipmentAction> Actions, List<ShipmentAccessRule> Rules)> LoadMatrixAsync(
        CancellationToken cancellationToken)
    {
        _actions ??= await dbContext.ShipmentActions
            .AsNoTracking()
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(cancellationToken);

        _rules ??= await dbContext.ShipmentAccessRules
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (_actions, _rules);
    }
}
