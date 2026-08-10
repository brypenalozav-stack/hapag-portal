namespace HapagPortal.Application.Common.Interfaces;

/// <summary>Datos de una notificación a publicar.</summary>
public sealed record NotificationRequest(
    string Type,
    string Title,
    string Body,
    Guid? UserId = null,
    string? RoleCode = null,
    string? DedupKey = null,
    string? Email = null);

/// <summary>
/// Publica notificaciones operativas (persistencia + email opcional). Con DedupKey evita duplicar
/// la misma alerta. Lo consumen los disparadores (plazos en riesgo, rechazos de transmisión).
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
