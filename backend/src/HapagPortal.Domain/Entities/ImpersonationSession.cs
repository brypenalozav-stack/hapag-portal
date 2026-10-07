using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Sesión de «Vista como cliente» (M8-08): un usuario interno autorizado (actor) ve el portal con la visibilidad y
/// los permisos de un usuario cliente (sujeto) sin modificar sus credenciales. Registra inicio, término, duración,
/// motivo y la cantidad de solicitudes; cada solicitud queda además en <c>AuditLogs</c> con la identidad del actor.
/// </summary>
public sealed class ImpersonationSession : GuidEntity
{
    public Guid ActorUserId { get; set; }
    public required string ActorEmail { get; set; }
    public Guid SubjectUserId { get; set; }
    public required string SubjectEmail { get; set; }
    public Guid OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public required string Reason { get; set; }

    /// <summary><c>ImpersonationStatus</c>: Active, Ended o Expired.</summary>
    public required string Status { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? EndedAt { get; set; }

    /// <summary><c>ImpersonationEndReasons</c>: Manual, Logout, Expired, Replaced o Admin.</summary>
    public string? EndReason { get; set; }

    public int? DurationSeconds { get; set; }
    public int RequestCount { get; set; }
    public int BlockedCount { get; set; }
    public string? SourceAddress { get; set; }
}
