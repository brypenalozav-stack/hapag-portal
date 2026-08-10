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

        // Error genérico para no revelar si el email existe ni distinguir el motivo (BUG-11).
        var invalidRequest = Result.Failure(
            new Error("Auth.InvalidResetRequest", "The password reset request is invalid or has expired."));

        if (user is null || !user.IsActive)
            return invalidRequest;

        if (user.PasswordResetToken is null ||
            user.PasswordResetToken != request.Token)
        {
            return invalidRequest;
        }

        if (user.PasswordResetTokenExpiry is null ||
            user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return invalidRequest;
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
