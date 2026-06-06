namespace HapagPortal.Application.Auth.ConfirmEmail;

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
        var user = await dbContext.Users
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
            return Result.Failure(DomainErrors.User.NotFoundByEmail(request.Email));

        if (user.EmailConfirmationToken is null ||
            user.EmailConfirmationToken != request.Token)
        {
            return Result.Failure(
                new Error("Auth.InvalidToken", "The email confirmation token is invalid."));
        }

        if (user.Client is null)
            return Result.Failure(DomainErrors.Client.NotFound(user.ClientId ?? Guid.Empty));

        user.Client.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
