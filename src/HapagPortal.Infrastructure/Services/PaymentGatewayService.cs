using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Services;

public sealed class PaymentGatewayService(ILogger<PaymentGatewayService> logger) : IPaymentGatewayService
{
    public Task<PaymentGatewayResult> InitiatePaymentAsync(
        InitiatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Payment gateway initiated (placeholder) - Method: {Method}, Amount: {Amount} {Currency}",
            request.PaymentMethod,
            request.Amount,
            request.Currency);

        var externalReference = $"SIM-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
        var result = new PaymentGatewayResult(
            Success: true,
            RedirectUrl: $"{request.ReturnUrl}?ref={externalReference}",
            ExternalReference: externalReference,
            ErrorMessage: null);

        return Task.FromResult(result);
    }

    public Task<PaymentGatewayStatus> CheckPaymentStatusAsync(
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Payment status check (placeholder) - Reference: {Reference}",
            externalReference);

        var status = new PaymentGatewayStatus(
            Status: "Completed",
            TransactionId: $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            ConfirmedAt: DateTime.UtcNow);

        return Task.FromResult(status);
    }
}
