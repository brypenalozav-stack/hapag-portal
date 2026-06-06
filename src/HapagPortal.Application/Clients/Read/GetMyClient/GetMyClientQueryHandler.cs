namespace HapagPortal.Application.Clients.Read.GetMyClient;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetMyClientQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyClientQuery, ClientResponseDto>
{
    public async Task<Result<ClientResponseDto>> Handle(
        GetMyClientQuery request,
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
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == user.ClientId, cancellationToken);

        if (client is null)
            return Result<ClientResponseDto>.Failure(DomainErrors.Client.NotFound(user.ClientId.Value));

        return Result<ClientResponseDto>.Success(
            ClientResponseDto.FromClient(client));
    }
}
