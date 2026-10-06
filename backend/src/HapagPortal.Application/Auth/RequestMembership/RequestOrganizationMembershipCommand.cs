namespace HapagPortal.Application.Auth.RequestMembership;

using FluentValidation;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Registro de un usuario que pide vincularse a una organización ya registrada (M1-08). Queda
/// pendiente hasta que un administrador de esa organización lo apruebe; mientras, no ingresa.
/// </summary>
public sealed record RequestOrganizationMembershipCommand(
    string TaxId,
    string Country,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone) : ICommand<JoinRequestSubmittedDto>;

public sealed record JoinRequestSubmittedDto(Guid UserId, string OrganizationName, string Status);

public sealed class RequestOrganizationMembershipCommandValidator
    : AbstractValidator<RequestOrganizationMembershipCommand>
{
    public RequestOrganizationMembershipCommandValidator()
    {
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country)
            .NotEmpty()
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithMessage("Country must be 'CL' or 'BO'.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class RequestOrganizationMembershipCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<RequestOrganizationMembershipCommand, JoinRequestSubmittedDto>
{
    public async Task<Result<JoinRequestSubmittedDto>> Handle(
        RequestOrganizationMembershipCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var taxId = request.TaxId.Trim();

        var organization = await dbContext.Clients
            .FirstOrDefaultAsync(c => c.TaxId == taxId && c.Country == request.Country, cancellationToken);

        if (organization is null ||
            !organization.IsActive ||
            organization.RegistrationStatus == OrganizationStatus.Rejected ||
            organization.OrganizationType == OrganizationTypes.Internal)
        {
            return Result<JoinRequestSubmittedDto>.Failure(DomainErrors.Organization.NotFoundByTaxId);
        }

        var emailExists = await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailExists)
            return Result<JoinRequestSubmittedDto>.Failure(
                new Error("User.EmailExists", $"A user with email '{email}' already exists."));

        var user = new User
        {
            ClientId = organization.Id,
            Email = email,
            Username = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            UserType = UserTypes.Client,
            Country = organization.Country,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone,
            IsActive = true,
            MembershipStatus = MembershipStatus.Pending
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Bandeja de aprobaciones: se avisa a cada administrador activo de la organización (M1-08, M1-25).
        await OrganizationNotifier.NotifyAdminsAsync(
            dbContext,
            notificationPublisher,
            organization,
            NotificationTypes.JoinRequestReceived,
            "Nueva solicitud de vinculación",
            $"{user.FirstName} {user.LastName} ({user.Email}) solicita vincularse a {organization.Name}.",
            cancellationToken,
            dedupKeyPrefix: $"join-request:{user.Id}");

        return Result<JoinRequestSubmittedDto>.Success(
            new JoinRequestSubmittedDto(user.Id, organization.Name, user.MembershipStatus));
    }
}
