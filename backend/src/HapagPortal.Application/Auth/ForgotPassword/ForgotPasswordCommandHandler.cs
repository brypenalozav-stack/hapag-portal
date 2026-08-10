namespace HapagPortal.Application.Auth.ForgotPassword;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class ForgotPasswordCommandHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService,
    IAppEnvironment environment)
    : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        string? devToken = null;

        if (user is not null)
        {
            var resetToken = Guid.NewGuid().ToString();

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await dbContext.SaveChangesAsync(cancellationToken);

            await emailService.SendEmailAsync(
                user.Email,
                "Reset Your Password - Hapag-Lloyd Portal",
                $"Use the following token to reset your password: {resetToken}",
                cancellationToken);

            // Atajo de desarrollo: exponer el token cuando no es producción (BUG-5).
            if (!environment.IsProduction)
                devToken = resetToken;
        }

        // Siempre se devuelve éxito para no revelar si el email existe.
        return Result<ForgotPasswordResponse>.Success(new ForgotPasswordResponse(devToken));
    }
}
