namespace HapagPortal.Application.Users.Create;

using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Users.Common;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string RoleCode,
    string? Phone,
    int? DisplayId,
    string? Country) : ICommand<UserListItemDto>;
