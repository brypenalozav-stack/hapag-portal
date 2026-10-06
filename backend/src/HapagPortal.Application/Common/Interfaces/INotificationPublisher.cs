namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Datos de una notificación a publicar. <c>Email</c> es la dirección a la que se enviaría por correo; el envío se
/// decide por la preferencia del destinatario para el tipo (M1-25). <c>Link</c> identifica el embarque o la gestión a
/// la que corresponde y <c>Action</c>, la acción que puede tomarse desde la bandeja.
/// </summary>
public sealed record NotificationRequest(
    string Type,
    string Title,
    string Body,
    Guid? UserId = null,
    string? RoleCode = null,
    string? DedupKey = null,
    string? Email = null,
    NotificationLink? Link = null,
    NotificationAction? Action = null);

/// <summary>
/// Gestión o embarque al que corresponde una notificación (M1-25): tipo (<c>NotificationEntityTypes</c>), su
/// identificador, una referencia legible (número de solicitud, de pago, de documento) y el BL cuando aplica.
/// </summary>
public sealed record NotificationLink(
    string EntityType,
    string EntityId,
    string? Reference = null,
    string? BlNumber = null);

/// <summary>Acción que puede tomarse desde la notificación (<c>NotificationActionTypes</c>) sobre su destino.</summary>
public sealed record NotificationAction(string Type, string TargetId);

/// <summary>
/// Publica notificaciones operativas (persistencia + email opcional). Con DedupKey evita duplicar
/// la misma alerta. Lo consumen los disparadores (plazos en riesgo, rechazos de transmisión).
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
