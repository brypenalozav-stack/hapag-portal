namespace HapagPortal.Application.Notifications.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Notificación de la bandeja (M1-25): módulo de origen, la gestión o el embarque al que corresponde (<see cref="Link"/>)
/// y la acción disponible desde la notificación (<see cref="Action"/>).
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt,
    string Module = NotificationModules.General,
    DateTime? ReadAt = null,
    NotificationLinkDto? Link = null,
    NotificationActionDto? Action = null,
    bool EmailSent = false);

/// <summary>Gestión o embarque de la notificación (<c>NotificationEntityTypes</c>).</summary>
public sealed record NotificationLinkDto(string EntityType, string EntityId, string? Reference, string? BlNumber);

/// <summary>
/// Acción de la notificación (<c>NotificationActionTypes</c>). <see cref="Available"/> = la gestión sigue pendiente; una
/// vez resuelta por cualquier vía la acción ya no se ofrece (<see cref="ResolvedAt"/>).
/// </summary>
public sealed record NotificationActionDto(string Type, string TargetId, bool Available, DateTime? ResolvedAt);

/// <summary>Preferencia de correo de un tipo de notificación para el usuario actual (M1-25).</summary>
public sealed record NotificationPreferenceDto(
    string Type,
    string Module,
    bool EmailAvailable,
    bool EmailMandatory,
    bool EmailDefault,
    bool EmailEnabled,
    bool IsCustomized);

public static class NotificationInbox
{
    /// <summary>Notificaciones dirigidas al usuario actual o a alguno de sus roles.</summary>
    public static IQueryable<Notification> ForCurrentUser(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        var userId = currentUser.UserId;
        var roles = currentUser.Roles.ToList();

        return dbContext.Notifications
            .Where(n => (n.UserId != null && n.UserId == userId)
                        || (n.RoleCode != null && roles.Contains(n.RoleCode)));
    }

    public static NotificationDto ToDto(Notification n) => new(
        n.Id,
        n.Type,
        n.Title,
        n.Body,
        n.ReadAt != null,
        n.CreatedAt,
        n.Module ?? NotificationTypes.InfoOf(n.Type).Module,
        n.ReadAt,
        n.EntityType is null || n.EntityId is null ? null : new NotificationLinkDto(n.EntityType, n.EntityId, n.EntityReference, n.BlNumber),
        n.ActionType is null || n.ActionTargetId is null
            ? null
            : new NotificationActionDto(n.ActionType, n.ActionTargetId, n.ActionResolvedAt is null, n.ActionResolvedAt),
        n.EmailSent);

    /// <summary>
    /// Marca como resuelta la acción pendiente de las notificaciones que apuntan a una gestión (p. ej. la solicitud de
    /// vinculación ya decidida): la bandeja deja de ofrecerla. No guarda: lo hace el llamador.
    /// </summary>
    public static async Task ResolveActionAsync(
        IApplicationDbContext dbContext,
        string actionType,
        string targetId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var pending = await dbContext.Notifications
            .Where(n => n.ActionType == actionType && n.ActionTargetId == targetId && n.ActionResolvedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in pending)
            notification.ActionResolvedAt = now;
    }
}
