namespace HapagPortal.Application.Auth.ResetPassword;

using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class ResetPasswordCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return Result.Failure(DomainErrors.User.NotFoundByEmail(request.Email));

        if (!user.IsActive)
            return Result.Failure(DomainErrors.User.Inactive);

        if (user.PasswordResetToken is null ||
            user.PasswordResetToken != request.Token)
        {
            return Result.Failure(
                new Error("Auth.InvalidToken", "The password reset token is invalid."));
        }

        if (user.PasswordResetTokenExpiry is null ||
            user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return Result.Failure(
                new Error("Auth.TokenExpired", "The password reset token has expired."));
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        // Revocar sesiones activas al cambiar la contrasena (BUG-10).
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
