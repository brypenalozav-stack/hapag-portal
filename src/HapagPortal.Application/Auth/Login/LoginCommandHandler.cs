namespace HapagPortal.Application.Auth.Login;

using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.InvalidCredentials);

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.Inactive);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Failure(DomainErrors.User.InvalidCredentials);

        var roles = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleName)
            .ToListAsync(cancellationToken);

        var token = jwtTokenService.GenerateToken(user, roles);
        var refreshToken = jwtTokenService.GenerateRefreshToken();

        user.LastLoginAt = DateTime.UtcNow;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await dbContext.SaveChangesAsync(cancellationToken);

        var client = user.Client;
        var primaryRole = roles.Contains("Admin") ? "ADMIN" : "USER";
        var clientType = client?.ClientType switch
        {
            "Agent" => "AGENT",
            _ => "CLIENT"
        };

        var userDto = new ClientResponseDto(
            user.Id,
            client?.Name ?? user.Username,
            user.Email,
            client?.TaxId ?? string.Empty,
            client?.Phone,
            user.Country,
            clientType,
            primaryRole,
            user.IsActive,
            user.CreatedAt);

        const int expirationMinutes = 60;

        return Result<AuthResponseDto>.Success(
            new AuthResponseDto(token, expirationMinutes, userDto, refreshToken));
    }
}
