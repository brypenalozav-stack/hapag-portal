namespace HapagPortal.Application.Notifications.UnreadCount;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record GetUnreadCountQuery : IQuery<int>;

/// <summary>No leídas por módulo (M1-25) y cuántas tienen una acción pendiente.</summary>
public sealed record GetUnreadSummaryQuery : IQuery<UnreadSummaryDto>;

public sealed record UnreadSummaryDto(int Count, IReadOnlyDictionary<string, int> ByModule, int Actionable);

public sealed class GetUnreadCountQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetUnreadCountQuery, int>
{
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await NotificationInbox.ForCurrentUser(dbContext, currentUser).AsNoTracking()
            .Where(n => n.ReadAt == null)
            .CountAsync(cancellationToken);

        return Result<int>.Success(count);
    }
}

public sealed class GetUnreadSummaryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetUnreadSummaryQuery, UnreadSummaryDto>
{
    public async Task<Result<UnreadSummaryDto>> Handle(GetUnreadSummaryQuery request, CancellationToken cancellationToken)
    {
        var unread = await NotificationInbox.ForCurrentUser(dbContext, currentUser).AsNoTracking()
            .Where(n => n.ReadAt == null)
            .Select(n => new { n.Type, n.Module, Actionable = n.ActionType != null && n.ActionResolvedAt == null })
            .ToListAsync(cancellationToken);

        var byModule = unread
            .GroupBy(n => n.Module ?? NotificationTypes.InfoOf(n.Type).Module)
            .ToDictionary(g => g.Key, g => g.Count());

        return Result<UnreadSummaryDto>.Success(new UnreadSummaryDto(unread.Count, byModule, unread.Count(n => n.Actionable)));
    }
}
