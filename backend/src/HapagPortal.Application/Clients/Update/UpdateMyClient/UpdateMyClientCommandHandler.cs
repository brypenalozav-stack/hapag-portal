namespace HapagPortal.Application.Clients.Update.UpdateMyClient;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateMyClientCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateMyClientCommand, ClientResponseDto>
{
    public async Task<Result<ClientResponseDto>> Handle(
        UpdateMyClientCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<ClientResponseDto>.Failure(
                new Error("Error.Unauthorized", "User is not authenticated."));

        var userId = currentUserService.UserId.Value;

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<ClientResponseDto>.Failure(DomainErrors.User.NotFound(userId));

        if (user.ClientId is null)
            return Result<ClientResponseDto>.Failure(DomainErrors.Client.NotFound(Guid.Empty));

        var client = await dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == user.ClientId, cancellationToken);

        if (client is null)
            return Result<ClientResponseDto>.Failure(DomainErrors.Client.NotFound(user.ClientId.Value));

        if (request.Phone is not null)
            client.Phone = request.Phone;

        if (request.Address is not null)
            client.Address = request.Address;

        if (request.City is not null)
            client.City = request.City;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ClientResponseDto>.Success(
            ClientResponseDto.FromClient(client));
    }
}
