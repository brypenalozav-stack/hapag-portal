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

    // Fase 1 Ola C (M3-04, M3-05, M8-01): contenedor, tarifa aplicada y derecho a cambio gratuito.
    public string? ContainerNumber { get; set; }
    public string? TariffCode { get; set; }
    public string? TariffSource { get; set; }
    public bool IsFree { get; set; }

    /// <summary>Origen del derecho al cambio gratuito: NEXUS (exención) o PORTAL (regla interna).</summary>
    public string? EntitlementSource { get; set; }
    public string? EntitlementReference { get; set; }
    public Guid? RequestedByClientId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public Guid? BatchId { get; set; }
    public DateTime? CompletedAt { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
