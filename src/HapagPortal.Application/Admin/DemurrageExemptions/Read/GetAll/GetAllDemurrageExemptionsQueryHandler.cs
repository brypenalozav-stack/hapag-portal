namespace HapagPortal.Application.Admin.DemurrageExemptions.Read.GetAll;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetAllDemurrageExemptionsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllDemurrageExemptionsQuery, List<DemurrageExemptionResponseDto>>
{
    public async Task<Result<List<DemurrageExemptionResponseDto>>> Handle(
        GetAllDemurrageExemptionsQuery request,
        CancellationToken cancellationToken)
    {
        var exemptions = await dbContext.DemurrageExemptions
            .AsNoTracking()
            .Where(de => de.IsActive)
            .Select(de => new DemurrageExemptionResponseDto(
                de.Id,
                de.ClientName,
                de.TaxId,
                de.Country,
                de.Reason,
                de.IsActive))
            .ToListAsync(cancellationToken);

        return Result<List<DemurrageExemptionResponseDto>>.Success(exemptions);
    }
}
