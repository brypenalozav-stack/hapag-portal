namespace HapagPortal.Application.Common.Interfaces;

using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;

/// <summary>
/// Tipo de cambio leído desde Nexus (M5-05) mediante <see cref="IExchangeRateProvider"/>: el portal usa
/// el valor informado y aprobado, y registra el tipo y su vigencia en cada transacción para auditoría.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Tipo vigente a la fecha. Misma moneda: tasa 1. Sin valor o no aprobado para transacciones:
    /// <c>ExchangeRate.NotFound</c> / <c>ExchangeRate.NotApproved</c>.
    /// </summary>
    Task<Result<ExchangeRateQuote>> GetQuoteAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>Registra (sin guardar) el tipo de cambio usado en una transacción.</summary>
    ExchangeRateRecord Record(
        string transactionType,
        Guid transactionId,
        ExchangeRateQuote quote,
        decimal sourceAmount,
        decimal convertedAmount);
}
