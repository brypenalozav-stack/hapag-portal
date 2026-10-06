using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

namespace HapagPortal.Domain.Access;

/// <summary>
/// Semántica de la matriz base de M1-11, sin acceso a datos. La usa el evaluador de accesos de
/// Application y la usarán los otorgamientos a terceros (M1-12 en adelante).
/// </summary>
public static class AccessLevelResolver
{
    /// <summary>
    /// O: permitido salvo que se haya retirado. X: nunca. X (o): solo con otorgamiento expreso
    /// vigente (y no retirado). Un nivel desconocido se trata como X (denegación por defecto).
    /// </summary>
    public static bool IsAllowed(string level, bool explicitlyGranted = false, bool explicitlyWithdrawn = false) =>
        level switch
        {
            AccessLevels.Allowed => !explicitlyWithdrawn,
            AccessLevels.OnGrant => explicitlyGranted && !explicitlyWithdrawn,
            _ => false
        };

    /// <summary>
    /// Un permiso solo puede otorgarse si el nivel base del receptor no es X y quien otorga lo
    /// posee sobre ese BL o booking.
    /// </summary>
    public static bool CanBeGranted(string targetLevel, bool grantorHasPermission) =>
        grantorHasPermission && targetLevel is AccessLevels.Allowed or AccessLevels.OnGrant;

    /// <summary>
    /// Una acción habilitada por un acceso otorgado (M1-12, M1-15, M1-17). Sin conjunto explícito
    /// aplica el nivel base del receptor (solo O); con conjunto explícito, las acciones elegidas cuyo
    /// nivel no sea X (O o X (o)), de modo que también se puede retirar una O. En ambos casos nunca
    /// fuera del <paramref name="ceiling"/>: lo que el otorgante posee sobre el embarque.
    /// </summary>
    public static bool IsAllowedByGrant(
        string level,
        string actionCode,
        IReadOnlyCollection<string>? explicitActions,
        IReadOnlyCollection<string>? ceiling)
    {
        if (ceiling is not null && !ceiling.Contains(actionCode))
            return false;

        return explicitActions is null
            ? level == AccessLevels.Allowed
            : explicitActions.Contains(actionCode) && level is AccessLevels.Allowed or AccessLevels.OnGrant;
    }

    /// <summary>
    /// Nivel de una acción para un rol. La regla específica del tipo de organización
    /// (excepciones de Freight Forwarder) prevalece sobre la general; sin regla, X.
    /// </summary>
    public static string ResolveLevel(
        IEnumerable<ShipmentAccessRule> rules,
        Guid actionId,
        string role,
        string? organizationType)
    {
        string? general = null;

        foreach (var rule in rules)
        {
            if (rule.ShipmentActionId != actionId || rule.Role != role)
                continue;

            if (rule.OrganizationType is null)
                general = rule.Level;
            else if (rule.OrganizationType == organizationType)
                return rule.Level;
        }

        return general ?? AccessLevels.Denied;
    }

    /// <summary>
    /// Columnas de la matriz que aplican a una organización sobre un embarque: agencias de aduanas
    /// y transportistas usan su propia columna; clientes y Freight Forwarders, sus roles en el BL.
    /// </summary>
    public static IReadOnlyList<string> MatrixColumnsFor(string? organizationType, IEnumerable<string> shipmentRoles) =>
        organizationType switch
        {
            OrganizationTypes.CustomsAgency => [ShipmentRoleCodes.CustomsAgency],
            OrganizationTypes.Carrier => [ShipmentRoleCodes.Carrier],
            _ => shipmentRoles.Distinct().ToList()
        };
}
