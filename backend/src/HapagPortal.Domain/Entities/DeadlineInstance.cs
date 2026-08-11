using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Plazo concreto calculado para un manifiesto o un B/L: fecha límite derivada del evento base
/// y la regla. El estado se recalcula respecto del momento actual (o del momento de cumplimiento).
/// </summary>
public sealed class DeadlineInstance : BaseAuditableEntity
{
    public Guid RuleId { get; set; }
    public Guid? ManifestId { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public DateTime BaseEventAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public required string Status { get; set; }

    public DeadlineRule Rule { get; set; } = null!;
    public CustomsManifest? Manifest { get; set; }
    public BillOfLading? BillOfLading { get; set; }
}
