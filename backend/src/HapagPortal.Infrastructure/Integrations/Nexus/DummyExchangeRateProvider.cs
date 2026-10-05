using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Tipos de cambio simulados de Nexus (CT-NEXUS). Determinista y con base USD: 1 USD = 950 CLP,
/// 6,91 BOB y 0,92 EUR. Los cruces se calculan a partir de la base y se redondean a 6 decimales.
/// Moneda desconocida: <c>Success(null)</c>.
/// </summary>
public sealed class DummyExchangeRateProvider(ILogger<DummyExchangeRateProvider> logger) : IExchangeRateProvider
{
    private static readonly Dictionary<string, decimal> UnitsPerUsd = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1m,
        ["CLP"] = 950m,
        ["BOB"] = 6.91m,
        ["EUR"] = 0.92m,
    };

    public Task<Result<ExchangeRateQuote?>> GetRateAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        ExchangeRateQuote? quote = null;

        if (UnitsPerUsd.TryGetValue(fromCurrency, out var from) && UnitsPerUsd.TryGetValue(toCurrency, out var to))
        {
            var rate = Math.Round(to / from, 6);
            quote = new ExchangeRateQuote(
                fromCurrency.ToUpperInvariant(),
                toCurrency.ToUpperInvariant(),
                rate,
                date,
                DummyNexusData.Source,
                Approved: true);
        }

        logger.LogDebug(
            "Tipo de cambio Nexus (dummy) - {From}->{To}, Date: {Date}, Rate: {Rate}",
            fromCurrency, toCurrency, date, quote?.Rate);

        return Task.FromResult(Result<ExchangeRateQuote?>.Success(quote));
    }
}
