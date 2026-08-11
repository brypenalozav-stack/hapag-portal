namespace HapagPortal.Application.Notifications.Common;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt);
