using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Rol de una organización sobre un BL/booking (M1-11): Customer, Shipper o Consignee.
/// Es la fuente de los embarques propios; los accesos otorgados a terceros (M1-12 en adelante)
/// se modelan aparte y se suman en el mismo evaluador de accesos.
/// El titular heredado del BL (<see cref="BillOfLading.ClientId"/>) se considera Customer.
/// </summary>
public sealed class ShipmentRole : BaseAuditableEntity
{
    public Guid BillOfLadingId { get; set; }
    public Guid ClientId { get; set; }
    public required string Role { get; set; }
    public required string Source { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
    public Client Client { get; set; } = null!;
}
