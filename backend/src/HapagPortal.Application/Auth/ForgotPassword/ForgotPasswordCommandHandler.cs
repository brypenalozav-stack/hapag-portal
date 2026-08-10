namespace HapagPortal.Application.Auth.ForgotPassword;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class ForgotPasswordCommandHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is not null)
        {
            var resetToken = Guid.NewGuid().ToString();

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await dbContext.SaveChangesAsync(cancellationToken);

            // El token se entrega SOLO por email. En local, EmailService lo escribe en el
            // log cuando no hay SMTP configurado. La respuesta HTTP nunca lo expone.
            await emailService.SendEmailAsync(
                user.Email,
                "Reset Your Password - Hapag-Lloyd Portal",
                $"Use the following token to reset your password: {resetToken}",
                cancellationToken);
        }

        // Respuesta siempre idéntica, exista o no el correo (anti-enumeración).
        return Result.Success();
    }
}
