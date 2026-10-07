using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Notifications.Preferences;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Notifications;

/// <summary>
/// Persiste la notificación en la bandeja (M1-25) con su módulo, la gestión a la que corresponde y la acción
/// disponible, y si se indica un email intenta enviarlo (best-effort) solo cuando el tipo admite correo y el
/// destinatario no lo desactivó en sus preferencias. Con DedupKey evita crear una notificación no leída duplicada
/// para el mismo evento.
/// </summary>
public sealed class NotificationPublisher(
    IApplicationDbContext dbContext,
    IEmailService emailService,
    ILogger<NotificationPublisher> logger) : INotificationPublisher
{
    public async Task PublishAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DedupKey))
        {
            var exists = await dbContext.Notifications.AnyAsync(
                n => n.DedupKey == request.DedupKey && n.ReadAt == null, cancellationToken);
            if (exists)
                return;
        }

        var info = NotificationTypes.InfoOf(request.Type);
        var sendEmail = !string.IsNullOrWhiteSpace(request.Email)
            && await EmailWantedAsync(info, request.UserId, cancellationToken);

        var notification = new Notification
        {
            UserId = request.UserId,
            RoleCode = request.RoleCode,
            Type = request.Type,
            Title = request.Title,
            Body = request.Body,
            DedupKey = request.DedupKey,
            Module = info.Module,
            EntityType = request.Link?.EntityType,
            EntityId = request.Link?.EntityId,
            EntityReference = request.Link?.Reference,
            BlNumber = request.Link?.BlNumber,
            ActionType = request.Action?.Type,
            ActionTargetId = request.Action?.TargetId
        };
        dbContext.Notifications.Add(notification);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!sendEmail)
            return;

        try
        {
            await emailService.SendEmailAsync(request.Email!, request.Title, request.Body, cancellationToken);
            notification.EmailSent = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // El email es best-effort; la notificación en la campana ya quedó registrada.
            logger.LogWarning(ex, "No se pudo enviar el email de notificación a {Email}", request.Email);
        }
    }

    private async Task<bool> EmailWantedAsync(NotificationTypeInfo info, Guid? userId, CancellationToken cancellationToken)
    {
        if (!info.EmailAvailable)
            return false;

        if (userId is null || info.EmailMandatory)
            return true;

        var preference = await dbContext.NotificationPreferences.AsNoTracking()
            .Where(p => p.UserId == userId.Value && p.NotificationType == info.Type)
            .Select(p => (bool?)p.EmailEnabled)
            .FirstOrDefaultAsync(cancellationToken);

        return NotificationEmailPolicy.Resolve(info, preference);
    }
}
