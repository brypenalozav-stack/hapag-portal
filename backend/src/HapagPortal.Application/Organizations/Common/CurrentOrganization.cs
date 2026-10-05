namespace HapagPortal.Application.Organizations.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Usuario actual y su organización, para las operaciones acotadas a la propia organización.</summary>
public sealed record OrganizationMembership(User User, Client Organization);

public static class CurrentOrganization
{
    /// <summary>
    /// Carga el usuario actual y su organización. Con <paramref name="requireApproved"/> exige que la
    /// organización esté aprobada (M1-07: no opera antes de la aprobación).
    /// </summary>
    public static async Task<Result<OrganizationMembership>> LoadAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        bool requireApproved,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<OrganizationMembership>.Failure(Error.Unauthorized);

        var userId = currentUserService.UserId.Value;

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || !user.IsActive || user.MembershipStatus != MembershipStatus.Active)
            return Result<OrganizationMembership>.Failure(Error.Unauthorized);

        if (user.ClientId is null)
            return Result<OrganizationMembership>.Failure(DomainErrors.Organization.NotFound(Guid.Empty));

        var organization = await dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == user.ClientId.Value, cancellationToken);

        if (organization is null)
            return Result<OrganizationMembership>.Failure(DomainErrors.Organization.NotFound(user.ClientId.Value));

        if (requireApproved && organization.RegistrationStatus != OrganizationStatus.Approved)
            return Result<OrganizationMembership>.Failure(DomainErrors.Organization.NotOperational);

        return Result<OrganizationMembership>.Success(new OrganizationMembership(user, organization));
    }
}
