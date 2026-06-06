namespace HapagPortal.Application.Common.Dtos;

public sealed record LocalChargeDto(
    Guid Id,
    Guid BlId,
    string BlNumber,
    string ChargeCode,
    string? Description,
    decimal Amount,
    string Currency,
    string Status,
    bool IsTaxable,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalAmount,
    string Country);
