using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class FAQ : BaseAuditableEntity
{
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public required string Category { get; set; }
    public required string Country { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
