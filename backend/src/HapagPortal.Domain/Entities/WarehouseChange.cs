using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class WarehouseChange : BaseAuditableEntity
{
    public required string FromWarehouse { get; set; }
    public required string ToWarehouse { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public required string Country { get; set; }
    public Guid BillOfLadingId { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
