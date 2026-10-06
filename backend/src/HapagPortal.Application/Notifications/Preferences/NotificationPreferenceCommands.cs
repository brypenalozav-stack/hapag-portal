namespace HapagPortal.Application.Notifications.Preferences;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Qué notificaciones recibe el usuario además por correo (M1-25): una fila por tipo del catálogo con su valor por
/// defecto, si admite correo, si el correo es obligatorio y la elección del usuario.
/// </summary>
public sealed record GetNotificationPreferencesQuery : IQuery<IReadOnlyList<NotificationPreferenceDto>>;

/// <summary>Cambia la preferencia de correo de uno o más tipos; <c>EmailEnabled</c> nulo vuelve al valor por defecto.</summary>
public sealed record UpdateNotificationPreferencesCommand(IReadOnlyList<NotificationPreferenceChange> Items)
    : ICommand<IReadOnlyList<NotificationPreferenceDto>>;

public sealed record NotificationPreferenceChange(string Type, bool? EmailEnabled);

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Items).NotNull().Must(i => i.Count is >= 1 and <= 100).WithMessage("Indicate between 1 and 100 preferences.");
        RuleForEach(x => x.Items).ChildRules(item => item.RuleFor(i => i.Type).NotEmpty().MaximumLength(40));
    }
}

internal static class NotificationPreferenceViews
{
    public static async Task<IReadOnlyList<NotificationPreferenceDto>> LoadAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var saved = await dbContext.NotificationPreferences.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        return NotificationTypes.Catalog
            .Select(t =>
            {
                var preference = saved.FirstOrDefault(p => p.NotificationType == t.Type);
                return new NotificationPreferenceDto(
                    t.Type,
                    t.Module,
                    t.EmailAvailable,
                    t.EmailMandatory,
                    t.EmailDefault,
                    NotificationEmailPolicy.Resolve(t, preference?.EmailEnabled),
                    preference is not null);
            })
            .ToList();
    }
}

/// <summary>Decide si una notificación también va por correo según el catálogo y la preferencia del usuario (M1-25).</summary>
public static class NotificationEmailPolicy
{
    public static bool Resolve(NotificationTypeInfo info, bool? preference) =>
        info.EmailAvailable && (info.EmailMandatory || (preference ?? info.EmailDefault));
}

public sealed class GetNotificationPreferencesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetNotificationPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    public async Task<Result<IReadOnlyList<NotificationPreferenceDto>>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Result<IReadOnlyList<NotificationPreferenceDto>>.Failure(Error.Unauthorized);

        return Result<IReadOnlyList<NotificationPreferenceDto>>.Success(
            await NotificationPreferenceViews.LoadAsync(dbContext, userId, cancellationToken));
    }
}

public sealed class UpdateNotificationPreferencesCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : ICommandHandler<UpdateNotificationPreferencesCommand, IReadOnlyList<NotificationPreferenceDto>>
{
    public async Task<Result<IReadOnlyList<NotificationPreferenceDto>>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Result<IReadOnlyList<NotificationPreferenceDto>>.Failure(Error.Unauthorized);

        foreach (var item in request.Items)
        {
            var info = NotificationTypes.Catalog.FirstOrDefault(t => t.Type == item.Type.Trim());
            if (info is null)
                return Result<IReadOnlyList<NotificationPreferenceDto>>.Failure(DomainErrors.Notification.UnknownType(item.Type));
            if (item.EmailEnabled == true && !info.EmailAvailable)
                return Result<IReadOnlyList<NotificationPreferenceDto>>.Failure(DomainErrors.Notification.EmailNotAvailable(info.Type));
            if (item.EmailEnabled == false && info.EmailMandatory)
                return Result<IReadOnlyList<NotificationPreferenceDto>>.Failure(DomainErrors.Notification.EmailMandatory(info.Type));
        }

        var saved = await dbContext.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var item in request.Items.GroupBy(i => i.Type.Trim()).Select(g => g.Last()))
        {
            var type = item.Type.Trim();
            var preference = saved.FirstOrDefault(p => p.NotificationType == type);

            if (item.EmailEnabled is null)
            {
                if (preference is not null)
                    dbContext.NotificationPreferences.Remove(preference);
                continue;
            }

            if (preference is null)
            {
                dbContext.NotificationPreferences.Add(new NotificationPreference
                {
                    UserId = userId,
                    NotificationType = type,
                    EmailEnabled = item.EmailEnabled.Value,
                    UpdatedAt = now
                });
            }
            else
            {
                preference.EmailEnabled = item.EmailEnabled.Value;
                preference.UpdatedAt = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<NotificationPreferenceDto>>.Success(
            await NotificationPreferenceViews.LoadAsync(dbContext, userId, cancellationToken));
    }
}
