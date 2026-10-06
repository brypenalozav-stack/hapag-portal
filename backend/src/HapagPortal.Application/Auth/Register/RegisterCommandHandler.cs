namespace HapagPortal.Application.Auth.Register;

using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Carriers;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class RegisterCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IEmailService emailService)
    : ICommandHandler<RegisterCommand, ClientResponseDto>
{
    public async Task<Result<ClientResponseDto>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var taxIdType = CountryCodes.GetTaxIdType(request.Country);
        var email = EmailNormalizer.Normalize(request.Email);

        // M1-09: si un cliente ya pre-creó el perfil (por correo o identificación tributaria), no se duplica: se dirige
        // al ingreso con la invitación recibida.
        if (await PreCreatedAccounts.ExistsAsync(dbContext, email, request.TaxId, request.Country, cancellationToken))
            return Result<ClientResponseDto>.Failure(DomainErrors.Registration.PreCreatedAccount);

        var exists = await dbContext.Clients
            .AnyAsync(c => c.TaxId == request.TaxId && c.Country == request.Country, cancellationToken);

        if (exists)
            return Result<ClientResponseDto>.Failure(DomainErrors.Client.AlreadyExists(request.TaxId));

        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
            return Result<ClientResponseDto>.Failure(
                new Error("User.EmailExists", $"A user with email '{email}' already exists."));

        // Clients.Email también tiene índice único; validarlo aquí evita un
        // DbUpdateException -> 500 al hacer SaveChanges (BUG-19).
        var clientEmailExists = await dbContext.Clients
            .AnyAsync(c => c.Email == email, cancellationToken);

        if (clientEmailExists)
            return Result<ClientResponseDto>.Failure(
                new Error("Client.EmailExists", $"A client with email '{email}' already exists."));

        var organizationType = string.IsNullOrWhiteSpace(request.OrganizationType)
            ? OrganizationTypes.FromLegacyClientType(request.ClientType)
            : request.OrganizationType;

        var clientType = string.IsNullOrWhiteSpace(request.ClientType)
            ? OrganizationTypes.ToLegacyClientType(organizationType)
            : request.ClientType;

        // M1-07: la organización no opera hasta la validación y aprobación interna (M8-04).
        var client = new Client
        {
            Name = request.Name,
            TaxId = request.TaxId,
            TaxIdType = taxIdType,
            Country = request.Country,
            Email = email,
            Phone = request.Phone,
            ClientType = clientType,
            AgentCode = request.AgentCode,
            IsActive = true,
            OrganizationType = organizationType,
            RegistrationStatus = OrganizationStatus.PendingValidation,
            OperatingCountries = request.Country
        };

        dbContext.Clients.Add(client);

        var isAgent = clientType == UserTypes.CustomsAgent;
        var userType = isAgent ? UserTypes.Agent : UserTypes.Client;

        var emailConfirmationToken = Guid.NewGuid().ToString();

        var user = new User
        {
            ClientId = client.Id,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Username = email,
            UserType = userType,
            Country = request.Country,
            IsActive = true,
            FirstName = request.ContactFirstName,
            LastName = request.ContactLastName,
            Phone = request.Phone,
            MembershipStatus = MembershipStatus.Active,
            EmailConfirmationToken = emailConfirmationToken,
            EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(48)
        };

        dbContext.Users.Add(user);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleName = userType
        };

        dbContext.UserRoles.Add(userRole);

        // El primer usuario administra la organización: usuarios y solicitudes de vinculación (M1-02, M1-08).
        dbContext.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleName = RoleCodes.OrgAdmin
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await emailService.SendEmailAsync(
            email,
            "Welcome to Hapag-Lloyd Portal",
            $"Hello {request.Name}, your account has been created successfully. Please confirm your email to activate your account. Your confirmation token is: {emailConfirmationToken}. Your organization registration is pending validation by Hapag-Lloyd; you will be notified when it is approved.",
            cancellationToken);

        // Mismo contrato que el login: User.Id y role normalizado (BUG-16).
        var responseType = client.ClientType is UserTypes.CustomsAgent or UserTypes.Agent
            ? "AGENT"
            : "CLIENT";

        return Result<ClientResponseDto>.Success(new ClientResponseDto(
            user.Id,
            client.Name,
            user.Email,
            client.TaxId,
            client.Phone,
            user.Country,
            responseType,
            "USER",
            user.IsActive,
            user.CreatedAt));
    }
}
