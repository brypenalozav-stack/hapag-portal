namespace HapagPortal.Application.Notifications.GetMy;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Bandeja del usuario actual (M1-25): notificaciones dirigidas a él o a alguno de sus roles, de todos los módulos, las
/// más recientes primero. Filtros por no leídas, tipo, módulo, BL y solo con acción pendiente.
/// </summary>
public sealed record GetMyNotificationsQuery(
    bool OnlyUnread = false,
    string? Type = null,
    string? Module = null,
    string? BlNumber = null,
    bool OnlyActionable = false,
    int Limit = 100) : IQuery<List<NotificationDto>>;

public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(x => x.Module)
            .Must(m => NotificationModules.All.Contains(m))
            .When(x => !string.IsNullOrWhiteSpace(x.Module))
            .WithMessage("Module is not valid.");
        RuleFor(x => x.Type).MaximumLength(40);
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
    }
}

public sealed class GetMyNotificationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    public async Task<Result<List<NotificationDto>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = NotificationInbox.ForCurrentUser(dbContext, currentUser).AsNoTracking();

        if (request.OnlyUnread)
            query = query.Where(n => n.ReadAt == null);

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            query = query.Where(n => n.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            // Las notificaciones anteriores a la Ola I no guardaban el módulo: se deduce de su tipo.
            var module = request.Module.Trim();
            var types = NotificationTypes.Catalog.Where(t => t.Module == module).Select(t => t.Type).ToList();
            query = query.Where(n => n.Module == module || (n.Module == null && types.Contains(n.Type)));
        }

        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var blNumber = request.BlNumber.Trim();
            query = query.Where(n => n.BlNumber == blNumber);
        }

        if (request.OnlyActionable)
            query = query.Where(n => n.ActionType != null && n.ActionResolvedAt == null);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return Result<List<NotificationDto>>.Success(items.Select(NotificationInbox.ToDto).ToList());
    }
}
