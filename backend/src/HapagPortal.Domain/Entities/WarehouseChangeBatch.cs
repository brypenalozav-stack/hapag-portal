using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Solicitud masiva de cambios de almacén (M3-05). Se procesa en segundo plano por tramos (NF-19) y el
/// cliente consulta su avance y resultado. El acceso a cada BL se valida al recibir la solicitud.
/// </summary>
public sealed class WarehouseChangeBatch : BaseAuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public required string Status { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SucceededItems { get; set; }
    public int FailedItems { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<WarehouseChangeBatchItem> Items { get; set; } = [];
}
