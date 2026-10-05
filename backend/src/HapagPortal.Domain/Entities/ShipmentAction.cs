using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Acción o tipo de información sobre un BL/booking con nivel base por rol (M1-11).
/// El código es estable; nombre, orden y vigencia se administran desde el portal interno.
/// </summary>
public sealed class ShipmentAction : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Kind { get; set; }
    public required string Scope { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ShipmentAccessRule> Rules { get; set; } = [];
}
