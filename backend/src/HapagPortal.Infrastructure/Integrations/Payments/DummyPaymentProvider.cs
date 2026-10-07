using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Proveedor de pago simulado, registrado con clave para Khipu, BancoChile, Santander y Bci. No verifica
/// notificaciones (<see cref="VerifiesNotifications"/> = false): el webhook simulado toma el estado del cuerpo y la
/// conciliación no lo consulta. Determinista: la referencia es <c>DUMMY-&lt;proveedor&gt;-&lt;referencia&gt;</c> y la
/// redirección vuelve directo a la página de resultado del portal.
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

    /// <summary>
    /// Estado simulado: una referencia de la pasarela que contiene "REJECT" da <c>Failed</c>; el resto,
    /// <c>Confirmed</c> con el monto iniciado. No se usa en el flujo (Dummy no verifica), solo en pruebas.
    /// </summary>
    public Task<Result<PaymentVerification>> GetStatusAsync(
        PaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_initiated.TryGetValue(request.ExternalReference, out var initiated))
        {
            logger.LogWarning(
                "Consulta de pago (dummy) sin pago iniciado - Provider: {Provider}, Ref: {Ref}",
                ProviderCode, request.ExternalReference);

            return Task.FromResult(Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(ProviderCode)));
        }

        var reject = request.ProviderReference?.Contains("REJECT", StringComparison.OrdinalIgnoreCase) == true;

        var verification = new PaymentVerification(
            initiated.ExternalReference,
            reject ? PaymentStatus.Failed : PaymentStatus.Confirmed,
            initiated.Amount,
            initiated.Currency,
            reject ? null : $"DUMMY-TXN-{initiated.ExternalReference}");

        logger.LogInformation(
            "Consulta de pago (dummy) - Provider: {Provider}, Ref: {Ref}, Status: {Status}",
            ProviderCode, request.ExternalReference, verification.Status);

        return Task.FromResult(Result<PaymentVerification>.Success(verification));
    }

    public Task<Result> CancelAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    /// <summary>
    /// Dummy no autentica ni interpreta notificaciones: el webhook simulado usa el secreto compartido y el cuerpo
    /// (ver <c>PaymentNotificationCommandHandler</c>).
    /// </summary>
    public Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(
        PaymentNotification notification,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<PaymentNotificationInfo>.Failure(Error.Unauthorized));
}
