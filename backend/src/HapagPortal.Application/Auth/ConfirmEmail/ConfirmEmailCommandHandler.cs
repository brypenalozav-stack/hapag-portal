namespace HapagPortal.Application.Auth.ConfirmEmail;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class ConfirmEmailCommandHandler(
    IApplicationDbContext dbContext)
    : ICommandHandler<ConfirmEmailCommand>
{
    public async Task<Result> Handle(
        ConfirmEmailCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        var user = await dbContext.Users
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return Result.Failure(DomainErrors.User.NotFoundByEmail(request.Email));

        if (user.EmailConfirmationToken is null ||
            user.EmailConfirmationToken != request.Token)
        {
            return Result.Failure(
                new Error("Auth.InvalidToken", "The email confirmation token is invalid."));
        }

        if (user.EmailConfirmationTokenExpiry is null ||
            user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
        {
            return Result.Failure(
                new Error("Auth.TokenExpired", "The email confirmation token has expired."));
        }

        // Marcar al usuario y, si tiene cliente asociado, también al cliente.
        // No falla si el usuario no tiene cliente (p.ej. un Admin) (BUG-12).
        user.IsEmailConfirmed = true;
        if (user.Client is not null)
            user.Client.IsEmailConfirmed = true;

        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiry = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
