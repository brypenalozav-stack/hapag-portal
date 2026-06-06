using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class AuditLog : GuidEntity
{
    public required string EntityName { get; set; }
    public required string EntityId { get; set; }
    public required string Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
}
