namespace HapagPortal.Application.Users.Common;

public sealed record UserListItemDto(
    Guid Id,
    int? DisplayId,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<string> Roles);
