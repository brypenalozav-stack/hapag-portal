namespace HapagPortal.Application.Auth.Login;

using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Carriers;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IPermissionResolver permissionResolver,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        var user = await dbContext.Users
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.InvalidCredentials);

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.Inactive);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Failure(DomainErrors.User.InvalidCredentials);

        // M1-08: una solicitud de vinculación no aprobada no da acceso a la organización.
        if (user.MembershipStatus == MembershipStatus.Pending)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.PendingApproval);

        if (user.MembershipStatus == MembershipStatus.Rejected)
            return Result<AuthResponseDto>.Failure(DomainErrors.User.MembershipRejected);

        var roles = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleName)
            .ToListAsync(cancellationToken);

        var permissions = await permissionResolver.ResolveAsync(roles, cancellationToken) ?? [];

        var token = jwtTokenService.GenerateToken(user, roles, permissions.ToList());
        var refreshToken = jwtTokenService.GenerateRefreshToken();

        var now = DateTime.UtcNow;
        user.LastLoginAt = now;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = now.AddDays(7);

        // M1-09: el primer ingreso de un transportista pre-creado lo vincula a lo que los clientes le asignaron.
        var carrierActivated = user.Client is not null
            && await CarrierActivation.ActivateOnFirstLoginAsync(dbContext, user, user.Client, now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (carrierActivated)
            await CarrierActivation.NotifyRequestersAsync(dbContext, notificationPublisher, user.Client!, cancellationToken);

        var client = user.Client;
        var primaryRole = roles.Contains("Admin") ? "ADMIN" : "USER";
        var clientType = client?.ClientType switch
        {
            "Agent" => "AGENT",
            "CustomsAgent" => "AGENT",
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

        // Estado de la organización para que la interfaz muestre, p. ej., el registro en revisión (M1-07).
        var organization = client is null
            ? null
            : OrganizationMapper.ToSummary(
                client, user, roles, permissions.Contains(AccessPermissions.OperateShipments));

        return Result<AuthResponseDto>.Success(
            new AuthResponseDto(token, expirationMinutes, userDto, refreshToken, organization));
    }
}
