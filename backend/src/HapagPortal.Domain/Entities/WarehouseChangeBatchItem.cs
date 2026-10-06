using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Línea de una solicitud masiva de cambio de almacén, con su resultado.</summary>
public sealed class WarehouseChangeBatchItem : GuidEntity
{
    public Guid BatchId { get; set; }
    public int LineNumber { get; set; }
    public required string BlNumber { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public string? ContainerNumber { get; set; }
    public string? FromWarehouse { get; set; }
    public required string ToWarehouse { get; set; }
    public string? TariffCode { get; set; }
    public required string Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? WarehouseChangeId { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public WarehouseChangeBatch Batch { get; set; } = null!;
}
