using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Autoasociación permanente de una organización a un BL consultado con acceso abierto (M1-18).
/// El BL aparece en su listado mientras el acceso abierto del titular siga activo, y la asociación
/// es requisito para que una agencia de aduanas o un transportista incorpore cargos al carro.
/// </summary>
public sealed class ShipmentAssociation : BaseAuditableEntity
{
    public Guid BillOfLadingId { get; set; }
    public Guid ClientId { get; set; }
    public Guid AssociatedByUserId { get; set; }
    public DateTime AssociatedAt { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
    public Client Client { get; set; } = null!;
}
