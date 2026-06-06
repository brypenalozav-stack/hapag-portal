using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class PaymentDetail : GuidEntity
{
    public required string ConceptType { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;
}
