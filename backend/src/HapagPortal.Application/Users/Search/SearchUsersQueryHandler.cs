namespace HapagPortal.Application.Users.Search;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Users.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class SearchUsersQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<SearchUsersQuery, PagedResult<UserListItemDto>>
{
    public async Task<Result<PagedResult<UserListItemDto>>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        var query = dbContext.Users.AsNoTracking();

        if (request.IsActive is not null)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                u.Email.ToLower().Contains(term) ||
                u.Username.ToLower().Contains(term));
        }

        // Filtro por rol: usuarios que tengan un UserRole con ese RoleName/Code.
        if (!string.IsNullOrWhiteSpace(request.RoleCode))
        {
            var role = request.RoleCode;
            var userIds = dbContext.UserRoles
                .Where(ur => ur.RoleName == role)
                .Select(ur => ur.UserId);
            query = query.Where(u => userIds.Contains(u.Id));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.DisplayId ?? int.MaxValue)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var ids = users.Select(u => u.Id).ToList();
        var rolesByUser = (await dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ids.Contains(ur.UserId))
                .Select(ur => new { ur.UserId, ur.RoleName })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

        var items = users.Select(u => new UserListItemDto(
            u.Id,
            u.DisplayId,
            string.Join(" ", new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
            u.Email,
            u.Phone,
            u.IsActive,
            rolesByUser.TryGetValue(u.Id, out var r) ? r : [])).ToList();

        return Result<PagedResult<UserListItemDto>>.Success(
            new PagedResult<UserListItemDto>(items, total, page, pageSize));
    }
}
