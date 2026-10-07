using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Solicitud masiva de generación de TATC para embarques de una misma localidad (M2-09), pensada para
/// clientes con alto volumen por un puerto (p. ej. la operación por Iquique). Se valida el acceso a cada BL
/// al recibirla y las líneas válidas se envían en una sola solicitud al sistema de TATC (CT-TATC). Se procesa
/// en la misma solicitud, por lo que se crea y se completa en el mismo instante.
/// </summary>
public sealed class TatcBatch : GuidEntity
{
    public Guid ClientId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public required string RequestedByEmail { get; set; }
    public required string Country { get; set; }
    public required string LocationCode { get; set; }
    public required string Status { get; set; }
    public string? SourceRequestId { get; set; }
    public string? ErrorCode { get; set; }
    public int TotalItems { get; set; }
    public int AcceptedItems { get; set; }
    public int RejectedItems { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<TatcBatchItem> Items { get; set; } = [];
}
