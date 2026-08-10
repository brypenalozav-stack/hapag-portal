namespace HapagPortal.Application.Notifications.MarkRead;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record MarkNotificationReadCommand(Guid Id) : ICommand;

public sealed class MarkNotificationReadCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken);

        if (notification is null)
            return Result.Failure(Error.NotFound("Notification"));

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
