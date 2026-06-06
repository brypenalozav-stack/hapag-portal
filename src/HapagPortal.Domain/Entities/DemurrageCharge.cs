using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class DemurrageCharge : BaseAuditableEntity
{
    public required string ContainerNumber { get; set; }
    public int FreeDays { get; set; }
    public int DemurrageDays { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public required string Status { get; set; }
    public bool IsExempt { get; set; }
    public string? ExemptReason { get; set; }
    public Guid BillOfLadingId { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
