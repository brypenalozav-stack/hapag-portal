namespace HapagPortal.Application.Auth.Register;

using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
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

        var exists = await dbContext.Clients
            .AnyAsync(c => c.TaxId == request.TaxId && c.Country == request.Country, cancellationToken);

        if (exists)
            return Result<ClientResponseDto>.Failure(DomainErrors.Client.AlreadyExists(request.TaxId));

        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
            return Result<ClientResponseDto>.Failure(
                new Error("User.EmailExists", $"A user with email '{request.Email}' already exists."));

        var client = new Client
        {
            Name = request.Name,
            TaxId = request.TaxId,
            TaxIdType = taxIdType,
            Country = request.Country,
            Email = request.Email,
            Phone = request.Phone,
            ClientType = request.ClientType,
            AgentCode = request.AgentCode,
            IsActive = true
        };

        dbContext.Clients.Add(client);

        var isAgent = request.ClientType == UserTypes.CustomsAgent;
        var userType = isAgent ? UserTypes.Agent : UserTypes.Client;

        var emailConfirmationToken = Guid.NewGuid().ToString();

        var user = new User
        {
            ClientId = client.Id,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Username = request.Email,
            UserType = userType,
            Country = request.Country,
            IsActive = true,
            EmailConfirmationToken = emailConfirmationToken
        };

        dbContext.Users.Add(user);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleName = userType
        };

        dbContext.UserRoles.Add(userRole);

        await dbContext.SaveChangesAsync(cancellationToken);

        await emailService.SendEmailAsync(
            request.Email,
            "Welcome to Hapag-Lloyd Portal",
            $"Hello {request.Name}, your account has been created successfully. Please confirm your email to activate your account. Your confirmation token is: {emailConfirmationToken}",
            cancellationToken);

        return Result<ClientResponseDto>.Success(
            ClientResponseDto.FromClient(client, userType));
    }
}
