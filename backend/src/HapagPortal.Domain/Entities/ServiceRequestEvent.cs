using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Hito de la línea de tiempo de una solicitud: transición de estado o nota, con fecha y actor.</summary>
public sealed class ServiceRequestEvent : GuidEntity
{
    public Guid ServiceRequestId { get; set; }
    public int Sequence { get; set; }
    public string? FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string ActorName { get; set; }

    /// <summary><c>ServiceRequestActorKinds</c>: Client, Internal o System.</summary>
    public required string ActorKind { get; set; }
    public string? Notes { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
}
