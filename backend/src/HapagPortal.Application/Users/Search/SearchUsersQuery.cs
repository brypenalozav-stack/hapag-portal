namespace HapagPortal.Application.Users.Search;

using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Users.Common;

/// <summary>Búsqueda de usuarios de la plataforma (pantalla de la consola interna).</summary>
public sealed record SearchUsersQuery(
    bool? IsActive,
    string? RoleCode,
    string? Search,
    int Page = 1,
    int PageSize = 10) : IQuery<PagedResult<UserListItemDto>>;
