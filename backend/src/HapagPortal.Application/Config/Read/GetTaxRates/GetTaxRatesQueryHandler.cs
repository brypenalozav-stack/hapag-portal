namespace HapagPortal.Application.Config.Read.GetTaxRates;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetTaxRatesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTaxRatesQuery, List<TaxRateResponseDto>>
{
    public async Task<Result<List<TaxRateResponseDto>>> Handle(
        GetTaxRatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.TaxConfigurations
            .AsNoTracking()
            .Where(tc => tc.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Country))
            query = query.Where(tc => tc.Country == request.Country);

        var taxRates = await query
            .Select(tc => new TaxRateResponseDto(
                tc.Id,
                tc.Country,
                tc.ServiceType,
                tc.TaxName,
                tc.TaxRate,
                tc.EffectiveFrom,
                tc.EffectiveTo))
            .ToListAsync(cancellationToken);

        return Result<List<TaxRateResponseDto>>.Success(taxRates);
    }
}
