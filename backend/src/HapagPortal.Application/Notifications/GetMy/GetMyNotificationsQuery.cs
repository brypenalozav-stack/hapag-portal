namespace HapagPortal.Application.Notifications.GetMy;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Notificaciones del usuario actual: dirigidas a él o a alguno de sus roles.</summary>
public sealed record GetMyNotificationsQuery(bool OnlyUnread = false) : IQuery<List<NotificationDto>>;

public sealed class GetMyNotificationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    public async Task<Result<List<NotificationDto>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var roles = currentUser.Roles.ToList();

        var query = dbContext.Notifications.AsNoTracking()
            .Where(n => (n.UserId != null && n.UserId == userId)
                        || (n.RoleCode != null && roles.Contains(n.RoleCode)));

        if (request.OnlyUnread)
            query = query.Where(n => n.ReadAt == null);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Body, n.ReadAt != null, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<NotificationDto>>.Success(items);
    }
}
