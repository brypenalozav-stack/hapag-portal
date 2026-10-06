using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Línea de una solicitud masiva de TATC con su resultado (aceptada, rechazada o fallida).</summary>
public sealed class TatcBatchItem : GuidEntity
{
    public Guid BatchId { get; set; }
    public int LineNumber { get; set; }
    public required string BlNumber { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public required string Status { get; set; }
    public string? ReasonCode { get; set; }

    public TatcBatch Batch { get; set; } = null!;
}
