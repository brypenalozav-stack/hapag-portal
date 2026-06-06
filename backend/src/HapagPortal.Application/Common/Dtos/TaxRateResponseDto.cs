namespace HapagPortal.Application.Common.Dtos;

public sealed record TaxRateResponseDto(
    Guid Id,
    string Country,
    string ServiceType,
    string TaxName,
    decimal TaxRate,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);
