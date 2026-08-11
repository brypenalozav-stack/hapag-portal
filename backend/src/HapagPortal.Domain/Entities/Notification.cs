using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Notificación operativa dirigida a un usuario concreto (UserId) o a todos los de un rol (RoleCode).
/// El destinatario la marca como leída (ReadAt). Puede tener una clave de deduplicación para no repetir.
/// </summary>
public sealed class Notification : BaseAuditableEntity
{
    public Guid? UserId { get; set; }
    public string? RoleCode { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? DedupKey { get; set; }
    public DateTime? ReadAt { get; set; }
}
