namespace HapagPortal.Application.Config.Read.GetCurrencies;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetCurrenciesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCurrenciesQuery, List<CurrencyResponseDto>>
{
    public async Task<Result<List<CurrencyResponseDto>>> Handle(
        GetCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        var currencies = await dbContext.Currencies
            .AsNoTracking()
            .Select(c => new CurrencyResponseDto(
                c.Id,
                c.Code,
                c.Name,
                c.Symbol,
                c.ExchangeRateToUSD,
                c.LastUpdated))
            .ToListAsync(cancellationToken);

        return Result<List<CurrencyResponseDto>>.Success(currencies);
    }
}
