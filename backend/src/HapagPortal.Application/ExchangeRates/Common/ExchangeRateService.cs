namespace HapagPortal.Application.ExchangeRates.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

/// <summary>
/// Lectura del tipo de cambio de Nexus (M5-05) con caché por solicitud: una misma evaluación usa un
/// único valor por par de monedas y fecha. Nexus administra el porcentaje de ajuste; aquí no se recalcula.
/// </summary>
public sealed class ExchangeRateService(
    IApplicationDbContext dbContext,
    IExchangeRateProvider exchangeRateProvider) : IExchangeRateService
{
    public const string IdentitySource = "IDENTITY";

    private readonly Dictionary<(string From, string To, DateOnly Date), Result<ExchangeRateQuote>> _cache = [];

    public async Task<Result<ExchangeRateQuote>> GetQuoteAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var from = fromCurrency.Trim().ToUpperInvariant();
        var to = toCurrency.Trim().ToUpperInvariant();

        if (from == to)
            return Result<ExchangeRateQuote>.Success(new ExchangeRateQuote(from, to, 1m, date, IdentitySource, true));

        if (_cache.TryGetValue((from, to, date), out var cached))
            return cached;

        var read = await exchangeRateProvider.GetRateAsync(from, to, date, cancellationToken);

        Result<ExchangeRateQuote> result = read.IsFailure
            ? Result<ExchangeRateQuote>.Failure(read.Error)
            : read.Value is null
                ? Result<ExchangeRateQuote>.Failure(DomainErrors.ExchangeRate.NotAvailable(from, to, date))
                : !read.Value.Approved
                    ? Result<ExchangeRateQuote>.Failure(DomainErrors.ExchangeRate.NotApproved(from, to))
                    : Result<ExchangeRateQuote>.Success(read.Value);

        _cache[(from, to, date)] = result;
        return result;
    }

    public ExchangeRateRecord Record(
        string transactionType,
        Guid transactionId,
        ExchangeRateQuote quote,
        decimal sourceAmount,
        decimal convertedAmount)
    {
        var record = new ExchangeRateRecord
        {
            TransactionType = transactionType,
            TransactionId = transactionId,
            FromCurrency = quote.FromCurrency,
            ToCurrency = quote.ToCurrency,
            Rate = quote.Rate,
            EffectiveDate = quote.EffectiveDate,
            Source = quote.Source,
            Approved = quote.Approved,
            SourceAmount = sourceAmount,
            ConvertedAmount = convertedAmount,
            CapturedAt = DateTime.UtcNow
        };

        dbContext.ExchangeRateRecords.Add(record);
        return record;
    }
}
