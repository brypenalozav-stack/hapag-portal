namespace HapagPortal.Application.Users.SetActive;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Activa o desactiva un usuario (la tabla muestra el estado).</summary>
public sealed record SetUserActiveCommand(Guid Id, bool IsActive) : ICommand;

public sealed class SetUserActiveCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<SetUserActiveCommand>
{
    public async Task<Result> Handle(
        SetUserActiveCommand request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
            return Result.Failure(DomainErrors.User.NotFound(request.Id));

        user.IsActive = request.IsActive;

        // Al desactivar, se revoca la sesión activa.
        if (!request.IsActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
