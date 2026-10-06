namespace HapagPortal.Application.Auth.Logout;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Cierre de sesión en el servidor (M1-10): revoca el refresh token del usuario autenticado o, si el
/// token de acceso ya expiró, el del refresh token recibido. Es idempotente y no revela si existía.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken = null) : ICommand;

public sealed class LogoutCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = userId is not null
            ? await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken)
            : string.IsNullOrWhiteSpace(request.RefreshToken)
                ? null
                : await dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user is not null && user.RefreshToken is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
