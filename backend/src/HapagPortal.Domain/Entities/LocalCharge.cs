using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class LocalCharge : BaseAuditableEntity
{
    public required string ChargeType { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid BillOfLadingId { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
}
