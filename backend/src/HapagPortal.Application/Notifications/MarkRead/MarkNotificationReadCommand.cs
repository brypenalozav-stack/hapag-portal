namespace HapagPortal.Application.Notifications.MarkRead;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Marca como leída una notificación de la propia bandeja (M1-25); una ajena responde NotFound.</summary>
public sealed record MarkNotificationReadCommand(Guid Id) : ICommand;

public sealed class MarkNotificationReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await NotificationInbox.ForCurrentUser(dbContext, currentUser)
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken);

        if (notification is null)
            return Result.Failure(DomainErrors.Notification.NotFound(request.Id));

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
