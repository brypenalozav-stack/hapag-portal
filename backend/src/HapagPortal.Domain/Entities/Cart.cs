using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Carro de compra persistente por usuario y organización (M5-01). Los ítems se agrupan por país y
/// moneda de pago en sub-carros con subtotal y cierre independientes (M5-08).
/// </summary>
public sealed class Cart : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }

    public ICollection<CartItem> Items { get; set; } = [];
}
