namespace HapagPortal.Application.Auth.RefreshToken;

using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var email = jwtTokenService.GetEmailFromExpiredToken(request.Token);

        if (email is null)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.InvalidCredentials);

        var user = await dbContext.Users
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.InvalidCredentials);

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.Inactive);

        var roles = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleName)
            .ToListAsync(cancellationToken);

        var token = jwtTokenService.GenerateToken(user, roles);

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
            new AuthResponseDto(token, expirationMinutes, userDto));
    }
}
