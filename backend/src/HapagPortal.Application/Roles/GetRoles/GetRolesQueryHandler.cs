namespace HapagPortal.Application.Roles.GetRoles;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetRolesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetRolesQuery, List<RoleDto>>
{
    public async Task<Result<List<RoleDto>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Code, r.Name, r.IsSystem))
            .ToListAsync(cancellationToken);

        return Result<List<RoleDto>>.Success(roles);
    }
}
