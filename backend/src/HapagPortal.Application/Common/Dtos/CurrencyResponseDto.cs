namespace HapagPortal.Application.Common.Dtos;

public sealed record CurrencyResponseDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRateToUSD,
    DateTime LastUpdated);
