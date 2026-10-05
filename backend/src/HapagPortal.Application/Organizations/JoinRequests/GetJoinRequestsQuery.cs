namespace HapagPortal.Application.Organizations.JoinRequests;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Bandeja de aprobaciones: solicitudes pendientes de vinculación a la propia organización (M1-08).</summary>
public sealed record GetJoinRequestsQuery : IQuery<List<JoinRequestDto>>;

public sealed class GetJoinRequestsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetJoinRequestsQuery, List<JoinRequestDto>>
{
    public async Task<Result<List<JoinRequestDto>>> Handle(
        GetJoinRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: false, cancellationToken);

        if (membership.IsFailure)
            return Result<List<JoinRequestDto>>.Failure(membership.Error);

        var organization = membership.Value.Organization;

        var pending = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.ClientId == organization.Id && u.MembershipStatus == MembershipStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<List<JoinRequestDto>>.Success(pending
            .Select(u => new JoinRequestDto(
                u.Id,
                u.Email,
                OrganizationMapper.FullNameOf(u),
                u.Phone,
                u.CreatedAt,
                organization.Id,
                organization.Name))
            .ToList());
    }
}
