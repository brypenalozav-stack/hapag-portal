using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Notifications;

/// <summary>
/// Persiste la notificación y, si se indica un email, intenta enviarlo (best-effort). Con DedupKey
/// evita crear una notificación no leída duplicada para el mismo evento.
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

        dbContext.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            RoleCode = request.RoleCode,
            Type = request.Type,
            Title = request.Title,
            Body = request.Body,
            DedupKey = request.DedupKey
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                await emailService.SendEmailAsync(request.Email!, request.Title, request.Body, cancellationToken);
            }
            catch (Exception ex)
            {
                // El email es best-effort; la notificación en la campana ya quedó registrada.
                logger.LogWarning(ex, "No se pudo enviar el email de notificación a {Email}", request.Email);
            }
        }
    }
}
