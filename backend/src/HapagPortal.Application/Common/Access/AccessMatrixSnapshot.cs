namespace HapagPortal.Application.Common.Access;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Matriz base de M1-11 cargada desde la base (acciones y reglas), con la semántica que aplican el
/// evaluador de accesos y los otorgamientos a terceros (M1-12 a M1-17). Sin estado entre solicitudes.
/// </summary>
public sealed class AccessMatrixSnapshot(IReadOnlyList<ShipmentAction> actions, IReadOnlyList<ShipmentAccessRule> rules)
{
    public IReadOnlyList<ShipmentAction> Actions { get; } = actions;

    public static async Task<AccessMatrixSnapshot> LoadAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var actions = await dbContext.ShipmentActions
            .AsNoTracking()
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(cancellationToken);

        var rules = await dbContext.ShipmentAccessRules
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new AccessMatrixSnapshot(actions, rules);
    }

    /// <summary>Acciones activas sobre el embarque: las únicas que un acceso otorgado puede habilitar.</summary>
    public IEnumerable<ShipmentAction> ShipmentScopedActions =>
        Actions.Where(a => a.IsActive && a.Scope == ShipmentActionScopes.Shipment);

    public string LevelOf(ShipmentAction action, string column, string? organizationType) =>
        AccessLevelResolver.ResolveLevel(rules, action.Id, column, organizationType);

    /// <summary>
    /// Acciones de los roles propios del embarque (unión de columnas, M1-11) más los datos ampliados
    /// hacia esos roles por otro rol del BL (M1-16).
    /// </summary>
    public List<string> AllowedForRoles(
        string? organizationType,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string>? widenedActions = null)
    {
        if (roles.Count == 0)
            return [];

        var columns = AccessLevelResolver.MatrixColumnsFor(organizationType, roles);

        return Actions
            .Where(a => a.IsActive && (
                columns.Any(column => AccessLevelResolver.IsAllowed(LevelOf(a, column, organizationType)))
                || (widenedActions?.Contains(a.Code) ?? false)))
            .Select(a => a.Code)
            .ToList();
    }

    /// <summary>
    /// Acciones que habilita un acceso otorgado al receptor evaluado con <paramref name="receiverRole"/>
    /// (Tercero o, en el acceso anticipado por booking, el rol asignado). Agencias y transportistas usan
    /// su propia columna. Ver <see cref="AccessLevelResolver.IsAllowedByGrant"/>.
    /// </summary>
    public List<string> AllowedByGrant(
        string? organizationType,
        string receiverRole,
        IReadOnlyCollection<string>? explicitActions,
        IReadOnlyCollection<string>? ceiling)
    {
        var columns = AccessLevelResolver.MatrixColumnsFor(organizationType, [receiverRole]);

        return ShipmentScopedActions
            .Where(a => columns.Any(column =>
                AccessLevelResolver.IsAllowedByGrant(LevelOf(a, column, organizationType), a.Code, explicitActions, ceiling)))
            .Select(a => a.Code)
            .ToList();
    }

    /// <summary>Acciones que el receptor puede recibir: nivel O o X (o) en su columna (nunca X).</summary>
    public List<string> GrantableTo(string? organizationType, string receiverRole)
    {
        var columns = AccessLevelResolver.MatrixColumnsFor(organizationType, [receiverRole]);

        return ShipmentScopedActions
            .Where(a => columns.Any(column =>
                LevelOf(a, column, organizationType) is AccessLevels.Allowed or AccessLevels.OnGrant))
            .Select(a => a.Code)
            .ToList();
    }

    /// <summary>
    /// Acción de administración evaluada a nivel de organización (sin BL): clientes y Freight
    /// Forwarders por sus columnas de parte del embarque, agencias y transportistas por la propia.
    /// </summary>
    public bool OrganizationCan(string? organizationType, string actionCode, IReadOnlyCollection<string>? roles = null)
    {
        var action = Actions.FirstOrDefault(a => a.Code == actionCode && a.IsActive);
        if (action is null)
            return false;

        var columns = AccessLevelResolver.MatrixColumnsFor(organizationType, roles ?? ShipmentRoleCodes.ShipmentParties);
        return columns.Any(column => AccessLevelResolver.IsAllowed(LevelOf(action, column, organizationType)));
    }

    /// <summary>
    /// Solo información y servicios del embarque (primera matriz de M1-11), sin acciones de
    /// administración de accesos: es lo que alcanza el acceso abierto por número de BL (M1-17).
    /// </summary>
    public List<string> InformationOnly(IEnumerable<string> codes)
    {
        var set = codes.ToHashSet(StringComparer.Ordinal);
        return Actions
            .Where(a => a.Category == ShipmentActionCategories.Information && set.Contains(a.Code))
            .Select(a => a.Code)
            .ToList();
    }

    /// <summary>
    /// Solo consulta de información del embarque (categoría información, tipo consulta): lo que la empresa matriz ve de
    /// los BL de una filial (M1-21), sin pagar, solicitar ni administrar accesos.
    /// </summary>
    public List<string> ViewOnly(IEnumerable<string> codes)
    {
        var set = codes.ToHashSet(StringComparer.Ordinal);
        return Actions
            .Where(a => a.IsActive
                && a.Category == ShipmentActionCategories.Information
                && a.Kind == ShipmentActionKinds.View
                && set.Contains(a.Code))
            .Select(a => a.Code)
            .ToList();
    }

    /// <summary>Ordena códigos según la matriz y descarta los desconocidos.</summary>
    public List<string> Ordered(IEnumerable<string> codes)
    {
        var set = codes.ToHashSet(StringComparer.Ordinal);
        return Actions.Where(a => set.Contains(a.Code)).Select(a => a.Code).ToList();
    }
}
