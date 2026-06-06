namespace HapagPortal.Application.Common.Dtos;

public sealed record PaymentResponseDto(
    Guid Id,
    string PaymentNumber,
    string Type,
    string Method,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    string BlNumber,
    Guid BlId,
    Guid ClientId,
    string? ClientName,
    string Country,
    string? ReceiptUrl,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    List<PaymentDetailDto>? Details);

public sealed record PaymentDetailDto(
    Guid Id,
    string ChargeCode,
    string? Description,
    decimal Amount,
    string Currency);
