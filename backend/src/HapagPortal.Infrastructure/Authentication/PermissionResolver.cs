using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace HapagPortal.Infrastructure.Authentication;

public sealed class PermissionResolver(IApplicationDbContext dbContext) : IPermissionResolver
{
    // Roles que reciben todos los permisos (comodín). Incluye el legado "Admin" del seed actual.
    private static readonly string[] WildcardRoles =
        [RoleCodes.Administrador, RoleCodes.SuperAdmin, "Admin"];

    public async Task<IReadOnlyList<string>> ResolveAsync(
        IEnumerable<string> roleCodes,
        CancellationToken cancellationToken = default)
    {
        var roles = roleCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roles.Overlaps(WildcardRoles))
        {
            return await dbContext.Permissions
                .AsNoTracking()
                .Select(p => p.Code)
                .ToListAsync(cancellationToken);
        }

        var query =
            from rp in dbContext.RolePermissions.AsNoTracking()
            join r in dbContext.Roles.AsNoTracking() on rp.RoleId equals r.Id
            join p in dbContext.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where roles.Contains(r.Code)
            select p.Code;

        return await query.Distinct().ToListAsync(cancellationToken);
    }
}
