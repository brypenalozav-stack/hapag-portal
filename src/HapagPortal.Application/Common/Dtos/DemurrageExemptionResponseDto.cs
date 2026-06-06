namespace HapagPortal.Application.Common.Dtos;

public sealed record DemurrageExemptionResponseDto(
    Guid Id,
    string ClientName,
    string TaxId,
    string Country,
    string? Reason,
    bool IsActive);
