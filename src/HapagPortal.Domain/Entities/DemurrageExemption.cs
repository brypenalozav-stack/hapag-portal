using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class DemurrageExemption : BaseAuditableEntity
{
    public required string ClientName { get; set; }
    public required string TaxId { get; set; }
    public required string Country { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
}
