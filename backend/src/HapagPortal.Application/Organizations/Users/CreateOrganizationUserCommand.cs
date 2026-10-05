namespace HapagPortal.Application.Organizations.Users;

using FluentValidation;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// El administrador de la organización crea un usuario de la MISMA organización con un perfil
/// (OrgAdmin, OrgOperator u OrgViewer). El usuario define su contraseña con un código de un solo uso.
/// </summary>
public sealed record CreateOrganizationUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Profile,
    string? Phone) : ICommand<OrganizationUserDto>;

public sealed class CreateOrganizationUserCommandValidator : AbstractValidator<CreateOrganizationUserCommand>
{
    public CreateOrganizationUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Profile)
            .Must(p => RoleCodes.OrganizationProfiles.Contains(p))
            .WithMessage("Profile must be 'OrgAdmin', 'OrgOperator' or 'OrgViewer'.");
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
    }
}

public sealed class CreateOrganizationUserCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher,
    IEmailService emailService)
    : ICommandHandler<CreateOrganizationUserCommand, OrganizationUserDto>
{
    private const int InvitationValidityHours = 48;

    public async Task<Result<OrganizationUserDto>> Handle(
        CreateOrganizationUserCommand request,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: true, cancellationToken);

        if (membership.IsFailure)
            return Result<OrganizationUserDto>.Failure(membership.Error);

        var organization = membership.Value.Organization;
        var email = EmailNormalizer.Normalize(request.Email);

        if (await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return Result<OrganizationUserDto>.Failure(
                new Error("User.EmailExists", $"A user with email '{email}' already exists."));

        // Contraseña aleatoria nunca comunicada: el usuario define la suya con el flujo de
        // restablecimiento (M1-10) usando este código de un solo uso.
        var invitationToken = Guid.NewGuid().ToString();

        var user = new User
        {
            ClientId = organization.Id,
            Email = email,
            Username = email,
            PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString("N") + "Aa1!"),
            UserType = UserTypes.Client,
            Country = organization.Country,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone,
            IsActive = true,
            MembershipStatus = MembershipStatus.Active,
            MembershipDecidedAt = DateTime.UtcNow,
            MembershipDecidedBy = currentUserService.Email,
            PasswordResetToken = invitationToken,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(InvitationValidityHours)
        };

        dbContext.Users.Add(user);
        await OrganizationProfileAssigner.AssignAsync(dbContext, user.Id, request.Profile, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await emailService.SendEmailAsync(
            email,
            "Invitación al portal Hapag-Lloyd",
            $"Ha sido registrado como usuario de {organization.Name}. Defina su contraseña con este código de un solo uso, válido por {InvitationValidityHours} horas: {invitationToken}",
            cancellationToken);

        return Result<OrganizationUserDto>.Success(OrganizationUserMapper.ToDto(user, request.Profile));
    }
}
