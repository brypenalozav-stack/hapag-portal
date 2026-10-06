namespace HapagPortal.Application.Notifications.MarkAllRead;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Marca como leídas todas las no leídas de la bandeja, o solo las de un módulo (M1-25).</summary>
public sealed record MarkAllNotificationsReadCommand(string? Module = null) : ICommand<int>;

public sealed class MarkAllNotificationsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<Result<int>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var query = NotificationInbox.ForCurrentUser(dbContext, currentUser).Where(n => n.ReadAt == null);

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            var module = request.Module.Trim();
            var types = NotificationTypes.Catalog.Where(t => t.Module == module).Select(t => t.Type).ToList();
            query = query.Where(n => n.Module == module || (n.Module == null && types.Contains(n.Type)));
        }

        var pending = await query.ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var n in pending)
            n.ReadAt = now;

        if (pending.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(pending.Count);
    }
}
