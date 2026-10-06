namespace HapagPortal.Application.Organizations.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Asigna el perfil de organización (M1-02) reemplazando el anterior, sin tocar otros roles.</summary>
public static class OrganizationProfileAssigner
{
    public static async Task AssignAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        string profile,
        CancellationToken cancellationToken)
    {
        var roleId = await dbContext.Roles
            .Where(r => r.Code == profile)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var current = await dbContext.UserRoles
            .Where(ur => ur.UserId == userId && RoleCodes.OrganizationProfiles.Contains(ur.RoleName))
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleName = profile, RoleId = roleId });
            return;
        }

        current.RoleName = profile;
        current.RoleId = roleId;
    }

    public static async Task<Dictionary<Guid, string?>> ProfilesByUserAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Select(ur => new { ur.UserId, ur.RoleName })
            .ToListAsync(cancellationToken);

        return userIds.ToDictionary(
            id => id,
            id => OrganizationMapper.ProfileOf(rows.Where(r => r.UserId == id).Select(r => r.RoleName)));
    }
}
