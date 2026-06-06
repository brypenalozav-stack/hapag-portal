using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class TaxConfiguration : BaseAuditableEntity
{
    public required string Country { get; set; }
    public required string ServiceType { get; set; }
    public required string TaxName { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
