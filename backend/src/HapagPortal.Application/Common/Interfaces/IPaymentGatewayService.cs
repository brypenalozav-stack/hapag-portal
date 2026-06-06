namespace HapagPortal.Application.Common.Interfaces;

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> InitiatePaymentAsync(
        InitiatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayStatus> CheckPaymentStatusAsync(
        string externalReference,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentGatewayResult(
    bool Success,
    string? RedirectUrl,
    string? ExternalReference,
    string? ErrorMessage);

public sealed record InitiatePaymentRequest(
    string PaymentMethod,
    decimal Amount,
    string Currency,
    string Description,
    string ReturnUrl);

public sealed record PaymentGatewayStatus(
    string Status,
    string? TransactionId,
    DateTime? ConfirmedAt);
