namespace HapagPortal.Application.Organizations.Users;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Usuarios vinculados a la propia organización, activos o desactivados (M1-02, M1-08).</summary>
public sealed record GetOrganizationUsersQuery : IQuery<List<OrganizationUserDto>>;

public sealed class GetOrganizationUsersQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetOrganizationUsersQuery, List<OrganizationUserDto>>
{
    public async Task<Result<List<OrganizationUserDto>>> Handle(
        GetOrganizationUsersQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: false, cancellationToken);

        if (membership.IsFailure)
            return Result<List<OrganizationUserDto>>.Failure(membership.Error);

        var organizationId = membership.Value.Organization.Id;

        var users = await dbContext.Users
            .AsNoTracking()
            // M3-17: los usuarios técnicos del canal Web Service los administra Hapag-Lloyd, no la organización.
            .Where(u => u.ClientId == organizationId && u.MembershipStatus == MembershipStatus.Active && u.UserType != UserTypes.Technical)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var profiles = await OrganizationProfileAssigner.ProfilesByUserAsync(
            dbContext, users.Select(u => u.Id).ToList(), cancellationToken);

        return Result<List<OrganizationUserDto>>.Success(users
            .Select(u => OrganizationUserMapper.ToDto(u, profiles.GetValueOrDefault(u.Id)))
            .ToList());
    }
}

public static class OrganizationUserMapper
{
    public static OrganizationUserDto ToDto(User user, string? profile) =>
        new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            OrganizationMapper.FullNameOf(user),
            user.Phone,
            profile,
            user.IsActive,
            user.MembershipStatus,
            user.LastLoginAt,
            user.CreatedAt);
}
