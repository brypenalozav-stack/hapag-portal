namespace HapagPortal.Application.Organizations.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Notifica a los administradores de una organización (bandeja de M1-25 y, según la preferencia de cada uno, correo),
/// con la gestión o el embarque al que corresponde y la acción disponible.
/// </summary>
public static class OrganizationNotifier
{
    public static async Task NotifyAdminsAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        Client organization,
        string type,
        string title,
        string body,
        CancellationToken cancellationToken,
        string? dedupKeyPrefix = null,
        NotificationLink? link = null,
        NotificationAction? action = null)
    {
        var adminIds = dbContext.UserRoles
            .Where(ur => ur.RoleName == RoleCodes.OrgAdmin)
            .Select(ur => ur.UserId);

        var admins = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.ClientId == organization.Id
                && u.IsActive
                && u.MembershipStatus == MembershipStatus.Active
                && adminIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var admin in admins)
        {
            await notificationPublisher.PublishAsync(
                new NotificationRequest(
                    type,
                    title,
                    body,
                    UserId: admin.Id,
                    DedupKey: dedupKeyPrefix is null ? null : $"{dedupKeyPrefix}:{admin.Id}",
                    Email: admin.Email,
                    Link: link,
                    Action: action),
                cancellationToken);
        }
    }
}
