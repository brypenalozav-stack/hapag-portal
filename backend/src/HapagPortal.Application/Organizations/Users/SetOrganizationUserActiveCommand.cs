namespace HapagPortal.Application.Organizations.Users;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Activa o desactiva un usuario de la MISMA organización; al desactivar se revoca su sesión.</summary>
public sealed record SetOrganizationUserActiveCommand(Guid Id, bool IsActive) : ICommand;

public sealed class SetOrganizationUserActiveCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<SetOrganizationUserActiveCommand>
{
    public async Task<Result> Handle(SetOrganizationUserActiveCommand request, CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: true, cancellationToken);

        if (membership.IsFailure)
            return Result.Failure(membership.Error);

        var (actor, organization) = membership.Value;

        if (request.Id == actor.Id)
            return Result.Failure(DomainErrors.Organization.CannotChangeOwnAccount);

        var user = await dbContext.Users.FirstOrDefaultAsync(
            u => u.Id == request.Id
                && u.ClientId == organization.Id
                && u.MembershipStatus == MembershipStatus.Active,
            cancellationToken);

        if (user is null)
            return Result.Failure(DomainErrors.User.NotFound(request.Id));

        user.IsActive = request.IsActive;

        if (!request.IsActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
