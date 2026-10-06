namespace HapagPortal.Application.Organizations.GetMine;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Organización del usuario: tipo, estado del registro, Match Code, países y perfil.</summary>
public sealed record GetMyOrganizationQuery : IQuery<OrganizationSummaryDto>;

public sealed class GetMyOrganizationQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyOrganizationQuery, OrganizationSummaryDto>
{
    public async Task<Result<OrganizationSummaryDto>> Handle(
        GetMyOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: false, cancellationToken);

        if (membership.IsFailure)
            return Result<OrganizationSummaryDto>.Failure(membership.Error);

        var (user, organization) = membership.Value;

        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleName)
            .ToListAsync(cancellationToken);

        return Result<OrganizationSummaryDto>.Success(OrganizationMapper.ToSummary(
            organization, user, roles, currentUserService.HasPermission(AccessPermissions.OperateShipments)));
    }
}
