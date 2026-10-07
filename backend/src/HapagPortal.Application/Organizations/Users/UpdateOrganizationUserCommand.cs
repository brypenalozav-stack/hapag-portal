namespace HapagPortal.Application.Organizations.Users;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Modifica datos y perfil de un usuario de la MISMA organización (M1-02).</summary>
public sealed record UpdateOrganizationUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Phone,
    string? Profile) : ICommand<OrganizationUserDto>;

public sealed class UpdateOrganizationUserCommandValidator : AbstractValidator<UpdateOrganizationUserCommand>
{
    public UpdateOrganizationUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Profile)
            .Must(p => RoleCodes.OrganizationProfiles.Contains(p))
            .WithMessage("Profile must be 'OrgAdmin', 'OrgOperator' or 'OrgViewer'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Profile));
    }
}

public sealed class UpdateOrganizationUserCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateOrganizationUserCommand, OrganizationUserDto>
{
    public async Task<Result<OrganizationUserDto>> Handle(
        UpdateOrganizationUserCommand request,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: true, cancellationToken);

        if (membership.IsFailure)
            return Result<OrganizationUserDto>.Failure(membership.Error);

        var (actor, organization) = membership.Value;

        // Solo usuarios de la misma organización; uno ajeno responde NotFound (NF-05).
        var user = await dbContext.Users.FirstOrDefaultAsync(
            u => u.Id == request.Id
                && u.ClientId == organization.Id
                && u.MembershipStatus == MembershipStatus.Active,
            cancellationToken);

        if (user is null)
            return Result<OrganizationUserDto>.Failure(DomainErrors.User.NotFound(request.Id));

        var profiles = await OrganizationProfileAssigner.ProfilesByUserAsync(dbContext, [user.Id], cancellationToken);
        var profile = profiles.GetValueOrDefault(user.Id);

        if (!string.IsNullOrWhiteSpace(request.Profile) && request.Profile != profile)
        {
            // Evita que el administrador se quite a sí mismo la administración por error.
            if (user.Id == actor.Id)
                return Result<OrganizationUserDto>.Failure(DomainErrors.Organization.CannotChangeOwnAccount);

            await OrganizationProfileAssigner.AssignAsync(dbContext, user.Id, request.Profile, cancellationToken);
            profile = request.Profile;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Phone = request.Phone;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<OrganizationUserDto>.Success(OrganizationUserMapper.ToDto(user, profile));
    }
}
