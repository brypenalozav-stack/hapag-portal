namespace HapagPortal.Application.Clients.Read.GetClientById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetClientByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetClientByIdQuery, ClientResponseDto>
{
    public async Task<Result<ClientResponseDto>> Handle(
        GetClientByIdQuery request,
        CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (client is null)
            return Result<ClientResponseDto>.Failure(DomainErrors.Client.NotFound(request.Id));

        return Result<ClientResponseDto>.Success(
            ClientResponseDto.FromClient(client));
    }
}
