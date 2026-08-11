namespace HapagPortal.Application.Customs.Manifests;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Customs.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record GetManifestsQuery(string? Direction) : IQuery<List<ManifestDto>>;

public sealed class GetManifestsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetManifestsQuery, List<ManifestDto>>
{
    public async Task<Result<List<ManifestDto>>> Handle(GetManifestsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.CustomsManifests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Direction))
            query = query.Where(m => m.Direction == request.Direction);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ManifestDto(
                m.Id, m.VesselImo, m.Voyage, m.Port, m.Direction,
                m.EstimatedArrival, m.EstimatedDeparture,
                m.Transmissions.Count))
            .ToListAsync(cancellationToken);

        return Result<List<ManifestDto>>.Success(items);
    }
}
