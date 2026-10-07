using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Nivel base (Allowed / Denied / OnGrant) de una acción para un rol de la matriz de M1-11.
/// <see cref="OrganizationType"/> nulo aplica a todos; con valor (p. ej. FreightForwarder)
/// es una excepción que prevalece sobre la regla general.
/// </summary>
public sealed class ShipmentAccessRule : BaseAuditableEntity
{
    public Guid ShipmentActionId { get; set; }
    public required string Role { get; set; }
    public string? OrganizationType { get; set; }
    public required string Level { get; set; }

    public ShipmentAction ShipmentAction { get; set; } = null!;
}
