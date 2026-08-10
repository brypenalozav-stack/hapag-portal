namespace HapagPortal.Application.Notifications.MarkAllRead;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record MarkAllNotificationsReadCommand : ICommand<int>;

public sealed class MarkAllNotificationsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<Result<int>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var roles = currentUser.Roles.ToList();

        var pending = await dbContext.Notifications
            .Where(n => n.ReadAt == null
                        && ((n.UserId != null && n.UserId == userId)
                            || (n.RoleCode != null && roles.Contains(n.RoleCode))))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var n in pending)
            n.ReadAt = now;

        if (pending.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(pending.Count);
    }
}
