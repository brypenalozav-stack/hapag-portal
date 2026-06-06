namespace HapagPortal.Application.Clients.Read.GetAllClients;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetAllClientsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllClientsQuery, List<ClientResponseDto>>
{
    public async Task<Result<List<ClientResponseDto>>> Handle(
        GetAllClientsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Clients
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Country))
            query = query.Where(c => c.Country == request.Country);

        var clients = await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var result = clients
            .Select(c => ClientResponseDto.FromClient(c))
            .ToList();

        return Result<List<ClientResponseDto>>.Success(result);
    }
}
