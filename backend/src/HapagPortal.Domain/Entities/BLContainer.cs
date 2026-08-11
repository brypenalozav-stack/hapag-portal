using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class BLContainer : BaseAuditableEntity
{
    public required string ContainerNumber { get; set; }
    public required string ContainerType { get; set; }
    public string? SealNumber { get; set; }
    public decimal? Weight { get; set; }
    public required string Status { get; set; }
    // Campos aduaneros (DCSA / ISO 6346). Aditivos.
    public string? IsoTypeCode { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? Tare { get; set; }
    public decimal? Vgm { get; set; }
    public int? PackageCount { get; set; }
    public bool IsShipperOwned { get; set; }
    public Guid BillOfLadingId { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
