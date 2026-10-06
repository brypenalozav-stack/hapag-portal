using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Notificación operativa dirigida a un usuario concreto (UserId) o a todos los de un rol (RoleCode).
/// El destinatario la marca como leída (ReadAt). Puede tener una clave de deduplicación para no repetir.
/// Bandeja de M1-25: el módulo de origen, la gestión o el embarque a los que corresponde (<see cref="EntityType"/>,
/// <see cref="EntityId"/>, <see cref="EntityReference"/>, <see cref="BlNumber"/>) y, cuando aplica, la acción que
/// puede tomarse desde la notificación (<see cref="ActionType"/> sobre <see cref="ActionTargetId"/>); la acción deja de
/// estar disponible cuando la gestión se resuelve por otra vía (<see cref="ActionResolvedAt"/>).
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

    /// <summary><c>NotificationModules</c> del tipo (catálogo de <c>NotificationTypes</c>).</summary>
    public string? Module { get; set; }

    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? EntityReference { get; set; }
    public string? BlNumber { get; set; }
    public string? ActionType { get; set; }
    public string? ActionTargetId { get; set; }
    public DateTime? ActionResolvedAt { get; set; }

    /// <summary>Se envió también por correo (según la preferencia del destinatario).</summary>
    public bool EmailSent { get; set; }
}
