namespace HapagPortal.Application.Notifications.UnreadCount;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record GetUnreadCountQuery : IQuery<int>;

public sealed class GetUnreadCountQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetUnreadCountQuery, int>
{
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var roles = currentUser.Roles.ToList();

        var count = await dbContext.Notifications.AsNoTracking()
            .Where(n => n.ReadAt == null
                        && ((n.UserId != null && n.UserId == userId)
                            || (n.RoleCode != null && roles.Contains(n.RoleCode))))
            .CountAsync(cancellationToken);

        return Result<int>.Success(count);
    }
}
