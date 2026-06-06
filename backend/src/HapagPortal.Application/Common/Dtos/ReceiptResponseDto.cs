namespace HapagPortal.Application.Common.Dtos;

public sealed record ReceiptResponseDto(
    Guid Id,
    string ReceiptNumber,
    Guid PaymentId,
    string PaymentNumber,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Country,
    DateTime IssuedAt);
