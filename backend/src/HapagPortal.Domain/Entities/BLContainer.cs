using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class BLContainer : BaseAuditableEntity
{
    public required string ContainerNumber { get; set; }
    public required string ContainerType { get; set; }
    public string? SealNumber { get; set; }
    public decimal? Weight { get; set; }
    public required string Status { get; set; }
    public Guid BillOfLadingId { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
