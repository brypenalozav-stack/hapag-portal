using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Línea de mercancía del BL (ConsignmentItem/CargoItem de DCSA eBL v3): HS, bultos, pesos.
/// </summary>
public sealed class BLCargoItem : BaseAuditableEntity
{
    public Guid BillOfLadingId { get; set; }
    public string? HsCode { get; set; }
    public string? Description { get; set; }
    public int? PackageCount { get; set; }
    public string? PackageCode { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? Volume { get; set; }
    public string? ShippingMarks { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
