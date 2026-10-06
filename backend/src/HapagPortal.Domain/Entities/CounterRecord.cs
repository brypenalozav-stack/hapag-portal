using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Registro de Counter Bolivia/Ultramar de un BL (M8-09): fecha de canje, recepción del HBL y marca de desconsolidado,
/// con el país de la operación. Las tablas de origen viven en Nexus: el registro se propaga por
/// <c>ICounterRecorder</c> y aquí queda la copia local con su estado de sincronización; cada cambio queda en el
/// registro de mantenedores (NF-15).
/// </summary>
public sealed class CounterRecord : BaseAuditableEntity
{
    public Guid BillOfLadingId { get; set; }
    public required string BlNumber { get; set; }
    public required string Country { get; set; }
    public DateOnly? ExchangeDate { get; set; }
    public bool HblReceived { get; set; }
    public DateOnly? HblReceivedAt { get; set; }
    public bool Deconsolidated { get; set; }
    public DateOnly? DeconsolidatedAt { get; set; }
    public string? Notes { get; set; }

    /// <summary><c>CounterSyncStatus</c>: Pending, Synced o Failed.</summary>
    public required string SyncStatus { get; set; }

    public DateTime? SyncedAt { get; set; }
    public string? SyncError { get; set; }
    public string? SourceReference { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public required string RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
