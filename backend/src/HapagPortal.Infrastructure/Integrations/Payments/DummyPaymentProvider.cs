using System.Globalization;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Proveedor de pago simulado (modo de prueba), registrado con clave para Khipu, BancoChile, Santander y Bci. Envía al
/// pagador a la página del simulador del portal (<c>/payments/simulator</c>), donde elige el resultado; el resultado se
/// guarda en <see cref="IPaymentSimulatorStore"/> y la consulta de estado lo informa. No verifica notificaciones
/// (<see cref="VerifiesNotifications"/> = false): el webhook simulado toma el estado del cuerpo. Determinista: la
/// referencia es <c>DUMMY-&lt;proveedor&gt;-&lt;referencia&gt;</c>. Sin estado propio: funciona igual tras un reinicio.
/// </summary>
public sealed class DummyPaymentProvider(
    string providerCode,
    ILogger<DummyPaymentProvider> logger,
    IPaymentSimulatorStore? simulatorStore = null) : ISimulatedPaymentProvider
{
    /// <summary>Ruta de la página del simulador en el sitio del portal.</summary>
    public const string SimulatorPath = "/payments/simulator";

    private readonly IPaymentSimulatorStore _store = simulatorStore ?? new InMemoryPaymentSimulatorStore();

    public string ProviderCode { get; } = providerCode;

    public bool VerifiesNotifications => false;

    public Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var initiation = new PaymentInitiation(
            ProviderReference: $"DUMMY-{ProviderCode.ToUpperInvariant()}-{request.ExternalReference}",
            ExternalReference: request.ExternalReference,
            Status: PaymentStatus.Pending,
            RedirectUrl: SimulatorUrl(request));

        logger.LogInformation(
            "Pago (dummy) iniciado - Provider: {Provider}, Ref: {Ref}, Amount: {Amount} {Currency}",
            ProviderCode, request.ExternalReference, request.Amount, request.Currency);

        return Task.FromResult(Result<PaymentInitiation>.Success(initiation));
    }

    /// <summary>
    /// Página del simulador en el mismo sitio que la URL de retorno (relativa si el retorno es relativo), con el
    /// proveedor, la referencia, el monto, la moneda y la URL de retorno (con <c>?ref=</c>, como el flujo real).
    /// </summary>
    public string SimulatorUrl(PaymentInitiationRequest request)
    {
        var origin = Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https"
            ? absolute.GetLeftPart(UriPartial.Authority)
            : string.Empty;

        var separator = request.ReturnUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var returnUrl = $"{request.ReturnUrl}{separator}ref={Uri.EscapeDataString(request.ExternalReference)}";

        return $"{origin}{SimulatorPath}" +
            $"?provider={Uri.EscapeDataString(ProviderCode)}" +
            $"&ref={Uri.EscapeDataString(request.ExternalReference)}" +
            $"&amount={request.Amount.ToString(CultureInfo.InvariantCulture)}" +
            $"&currency={Uri.EscapeDataString(request.Currency)}" +
            $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    /// <summary>
    /// Estado simulado según el resultado elegido en el simulador (<see cref="PaymentSimulatorOutcomes.StatusFor"/>);
    /// una referencia de la pasarela que contiene "REJECT" da <c>Failed</c>. Monto y moneda son los del pago del portal
    /// (<see cref="PaymentStatusRequest.ExpectedAmount"/>): no depende de memoria propia, así que funciona tras un reinicio.
    /// </summary>
    public Task<Result<PaymentVerification>> GetStatusAsync(
        PaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ExpectedAmount is not { } amount || string.IsNullOrWhiteSpace(request.ExpectedCurrency))
        {
            logger.LogWarning(
                "Consulta de pago (dummy) sin monto o moneda - Provider: {Provider}, Ref: {Ref}",
                ProviderCode, request.ExternalReference);

            return Task.FromResult(Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(ProviderCode)));
        }

        var reject = request.ProviderReference?.Contains("REJECT", StringComparison.OrdinalIgnoreCase) == true;
        var status = reject ? PaymentStatus.Failed : PaymentSimulatorOutcomes.StatusFor(_store.Outcome(request.ExternalReference));

        var verification = new PaymentVerification(
            request.ExternalReference,
            status,
            amount,
            request.ExpectedCurrency,
            status == PaymentStatus.Confirmed ? $"DUMMY-TXN-{request.ExternalReference}" : null);

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
