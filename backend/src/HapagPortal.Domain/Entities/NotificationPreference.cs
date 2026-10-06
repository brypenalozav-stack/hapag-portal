using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Preferencia de un usuario sobre un tipo de notificación (M1-25): si además de la bandeja la recibe por correo.
/// Sin fila para un tipo rige el valor por defecto del catálogo (<c>NotificationTypes.Catalog</c>).
/// </summary>
public sealed class NotificationPreference : GuidEntity
{
    public Guid UserId { get; set; }
    public required string NotificationType { get; set; }
    public bool EmailEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}
