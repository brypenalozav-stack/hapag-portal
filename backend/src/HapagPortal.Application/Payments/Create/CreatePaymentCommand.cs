namespace HapagPortal.Application.Payments.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CreatePaymentCommand(
    Guid BlId,
    string Type,
    string Method,
    List<PaymentDetailRequest>? Details,
    string[]? ChargeIds,
    string Country) : ICommand<PaymentResponseDto>;

public sealed record PaymentDetailRequest(
    string ConceptType,
    string Description,
    decimal Amount,
    string Currency);
