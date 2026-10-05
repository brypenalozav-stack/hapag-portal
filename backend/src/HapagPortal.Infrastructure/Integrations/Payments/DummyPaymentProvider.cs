using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Proveedor de pago simulado, registrado con clave para Khipu, BancoChile, Santander y Bci. No verifica
/// notificaciones (<see cref="VerifiesNotifications"/> = false): el webhook mantiene el comportamiento de hoy.
/// Determinista: la referencia del proveedor es <c>DUMMY-&lt;proveedor&gt;-&lt;referencia&gt;</c>; en la
/// verificación, un token que contiene "REJECT" da <c>Failed</c> y el resto <c>Confirmed</c>, con el monto
/// del pago iniciado. Una referencia que no se inició da <c>Integration.InvalidResponse</c>.
/// </summary>
public sealed class DummyPaymentProvider(string providerCode, ILogger<DummyPaymentProvider> logger) : IPaymentProvider
{
    private readonly ConcurrentDictionary<string, PaymentInitiationRequest> _initiated = new(StringComparer.OrdinalIgnoreCase);

    public string ProviderCode { get; } = providerCode;

    public bool VerifiesNotifications => false;

    public Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        _initiated[request.ExternalReference] = request;

        var initiation = new PaymentInitiation(
            ProviderReference: $"DUMMY-{ProviderCode.ToUpperInvariant()}-{request.ExternalReference}",
            ExternalReference: request.ExternalReference,
            Status: PaymentStatus.Pending,
            RedirectUrl: $"{request.ReturnUrl}?ref={Uri.EscapeDataString(request.ExternalReference)}");

        logger.LogInformation(
            "Pago (dummy) iniciado - Provider: {Provider}, Ref: {Ref}, Amount: {Amount} {Currency}",
            ProviderCode, request.ExternalReference, request.Amount, request.Currency);

        return Task.FromResult(Result<PaymentInitiation>.Success(initiation));
    }

    public Task<Result<PaymentVerification>> VerifyNotificationAsync(
        string notificationToken,
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        if (!_initiated.TryGetValue(externalReference, out var request))
        {
            logger.LogWarning(
                "Verificación de pago (dummy) sin pago iniciado - Provider: {Provider}, Ref: {Ref}",
                ProviderCode, externalReference);

            return Task.FromResult(Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(ProviderCode)));
        }

        var reject = notificationToken.Contains("REJECT", StringComparison.OrdinalIgnoreCase);

        var verification = new PaymentVerification(
            request.ExternalReference,
            reject ? PaymentStatus.Failed : PaymentStatus.Confirmed,
            request.Amount,
            request.Currency,
            reject ? null : $"DUMMY-TXN-{request.ExternalReference}");

        logger.LogInformation(
            "Verificación de pago (dummy) - Provider: {Provider}, Ref: {Ref}, Status: {Status}",
            ProviderCode, externalReference, verification.Status);

        return Task.FromResult(Result<PaymentVerification>.Success(verification));
    }
}
