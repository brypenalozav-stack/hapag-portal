namespace HapagPortal.Application.Common.Interfaces;

using HapagPortal.Domain.Entities;

/// <summary>
/// Punto único de autorización por embarque (M1-11, NF-05). Toda consulta o acción sobre un BL,
/// sus cargos, demurrage o pagos debe pasar por aquí; la interfaz solo oculta opciones.
/// Ola B (accesos a terceros) extiende <see cref="FilterAccessible"/> y la evaluación con los
/// otorgamientos vigentes, sin cambiar este contrato.
/// </summary>
public interface IShipmentAccessEvaluator
{
    /// <summary>Contexto del usuario actual: organización, estado y perfil.</summary>
    Task<AccessScope> GetScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>Restringe la consulta a los embarques accesibles por el alcance (vacía si no opera).</summary>
    IQueryable<BillOfLading> FilterAccessible(IQueryable<BillOfLading> source, AccessScope scope);

    /// <summary>Roles efectivos de la organización sobre cada BL indicado.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRolesAsync(
        AccessScope scope,
        IReadOnlyCollection<BillOfLading> billsOfLading,
        CancellationToken cancellationToken = default);

    /// <summary>Permisos efectivos sobre un BL; sin roles ni administración, ninguna acción.</summary>
    Task<ShipmentPermissionSet> EvaluateAsync(
        AccessScope scope,
        BillOfLading billOfLading,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Alcance del usuario. <see cref="IsOperational"/> exige usuario activo, vinculación aprobada y
/// organización aprobada. <see cref="IsAdmin"/> es el perfil interno con visibilidad total (M8-06).
/// <see cref="CanOperate"/> refleja el perfil del usuario (M1-02): sin él solo consulta.
/// </summary>
public sealed record AccessScope(
    Guid? UserId,
    Guid? OrganizationId,
    string? OrganizationType,
    bool IsAdmin,
    bool IsOperational,
    bool CanOperate)
{
    public static readonly AccessScope None = new(null, null, null, false, false, false);
}

/// <summary>Resultado de evaluar la matriz para un BL.</summary>
public sealed record ShipmentPermissionSet(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> AllowedActions,
    bool IsAdmin,
    bool CanOperate)
{
    public static readonly ShipmentPermissionSet None = new([], [], false, false);

    public bool HasAccess => IsAdmin || Roles.Count > 0;

    /// <summary>El rol (o la administración) permite la acción o el dato.</summary>
    public bool Can(string actionCode) => IsAdmin || AllowedActions.Contains(actionCode);

    /// <summary>Permite la acción y el perfil del usuario puede operar (pagar, solicitar, generar).</summary>
    public bool CanExecute(string actionCode) => Can(actionCode) && CanOperate;
}
