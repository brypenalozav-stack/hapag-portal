using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Tipo de cambio vigente entre dos monedas ISO 4217 (CT-NEXUS, <c>GET /exchange-rates</c>).
/// Sin valor para la fecha: <c>Success(null)</c>.
/// </summary>
public interface IExchangeRateProvider
{
    Task<Result<ExchangeRateQuote?>> GetRateAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly date,
        CancellationToken cancellationToken = default);
}

/// <summary>Tipo de cambio informado por el sistema origen (<c>Source</c>) y si está aprobado para transacciones.</summary>
public sealed record ExchangeRateQuote(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly EffectiveDate,
    string Source,
    bool Approved);
