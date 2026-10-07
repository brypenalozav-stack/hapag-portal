namespace HapagPortal.Application.Users.Search;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Users.Common;

/// <summary>Búsqueda de usuarios de la plataforma (pantalla de la consola interna).</summary>
public sealed record SearchUsersQuery(
    bool? IsActive,
    string? RoleCode,
    string? Search,
    int Page = 1,
    int PageSize = 10,
    string? Sort = null,
    string? Direction = null) : IQuery<PagedResult<UserListItemDto>>
{
    /// <summary>Columnas por las que se puede ordenar el listado (<c>sort</c>, con <c>direction</c> asc|desc).</summary>
    public static readonly SortMap<Domain.Entities.User> Sorts = new SortMap<Domain.Entities.User>()
        .Add("displayId", u => u.DisplayId ?? int.MaxValue)
        .Add("name", u => u.LastName ?? u.FirstName ?? u.Username)
        .Add("email", u => u.Email)
        .Add("isActive", u => u.IsActive)
        .Add("lastLoginAt", u => u.LastLoginAt);
}

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.Sort)
            .Must(SearchUsersQuery.Sorts.IsValid)
            .WithMessage($"Sort must be one of: {string.Join(", ", SearchUsersQuery.Sorts.Names)}.");

        RuleFor(x => x.Direction)
            .Must(SortMap<Domain.Entities.User>.IsValidDirection)
            .WithMessage("Direction must be 'asc' or 'desc'.");
    }
}
