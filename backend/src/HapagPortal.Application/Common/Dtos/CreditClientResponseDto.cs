namespace HapagPortal.Application.Common.Dtos;

public sealed record CreditClientResponseDto(
    Guid Id,
    string Name,
    string TaxId,
    string Country,
    decimal CreditLimit,
    decimal CreditUsed,
    string Currency,
    string Status,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    DateTime? ExpiresAt);
