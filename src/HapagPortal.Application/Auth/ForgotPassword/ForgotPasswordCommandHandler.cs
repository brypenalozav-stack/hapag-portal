namespace HapagPortal.Application.Auth.ForgotPassword;

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
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

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
        }

        // Always return success to avoid revealing whether the email exists
        return Result.Success();
    }
}
