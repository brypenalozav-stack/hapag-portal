using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Pasarela de pago en línea (CT-KHIPU, CT-BCH, CT-SANT, CT-BCI). Se registra como servicio con clave
/// (<c>Khipu</c>, <c>BancoChile</c>, <c>Santander</c>, <c>Bci</c>) y se resuelve con
/// <see cref="IPaymentProviderResolver"/>. Ver docs/integraciones/pasarelas-pago.md.
/// <para>
/// Regla común: el retorno del pagador y la notificación nunca confirman un pago por sí solos. La notificación
/// se autentica (<see cref="ReadNotificationAsync"/>) y el estado se toma siempre de la consulta a la pasarela
/// (<see cref="GetStatusAsync"/>), comparando referencia, monto y moneda con el pago del portal.
/// </para>
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Clave con la que el adaptador está registrado.</summary>
    string ProviderCode { get; }

    /// <summary>
    /// Indica si el adaptador habla con la pasarela real. Dummy = false: la notificación simulada trae el estado
    /// en el cuerpo y no hay conciliación. Real = true: notificación firmada y estado consultado a la pasarela.
    /// </summary>
    bool VerifiesNotifications { get; }

    /// <summary>Crea el pago en la pasarela y devuelve adónde enviar al pagador (URL o formulario firmado).</summary>
    Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estado vigente del pago en la pasarela. La referencia, el monto y la moneda devueltos son los que informa la
    /// pasarela: la comparación con el pago del portal la hace quien consulta.
    /// </summary>
    Task<Result<PaymentVerification>> GetStatusAsync(
        PaymentStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Anula en la pasarela un pago que aún no se pagó. Las pasarelas sin anulación devuelven éxito sin hacer nada:
    /// el pago vence solo en la pasarela.
    /// </summary>
    Task<Result> CancelAsync(
        PaymentStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Autentica una notificación recibida (firma sobre el cuerpo crudo) y extrae a qué pago se refiere. Falla con
    /// <c>Error.Unauthorized</c> si la firma no es válida. No cambia estados ni consulta el estado del pago.
    /// </summary>
    Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(
        PaymentNotification notification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Solicitud de pago. <c>ExternalReference</c> es la referencia del portal. <c>ReturnUrl</c> y <c>NotifyUrl</c> pueden
/// ser relativas: el adaptador Real las completa con <c>Payments:PublicBaseUrl</c> y <c>Payments:ApiPublicBaseUrl</c>.
/// </summary>
public sealed record PaymentInitiationRequest(
    string ExternalReference,
    decimal Amount,
    string Currency,
    string Subject,
    string ReturnUrl,
    string NotifyUrl,
    string? PayerTaxId,
    string? PayerEmail = null,
    string? PayerName = null,
    string? ClientIpAddress = null,
    string? ClientUserAgent = null);

/// <summary>
/// Pago creado en la pasarela: referencia de la pasarela y adónde se envía al pagador. Si <see cref="Form"/> viene,
/// el navegador envía ese formulario (POST firmado, típico de los botones bancarios) en vez de abrir la URL.
/// </summary>
public sealed record PaymentInitiation(
    string ProviderReference,
    string ExternalReference,
    string Status,
    string RedirectUrl,
    PaymentRedirectForm? Form = null);

/// <summary>Formulario que el navegador del pagador envía a la pasarela (método, URL de destino y campos ocultos).</summary>
public sealed record PaymentRedirectForm(
    string Method,
    string Action,
    IReadOnlyDictionary<string, string> Fields);

/// <summary>Pago a consultar o anular: referencia de la pasarela (si se conoce) y referencia del portal.</summary>
public sealed record PaymentStatusRequest(
    string? ProviderReference,
    string ExternalReference,
    decimal? ExpectedAmount = null,
    string? ExpectedCurrency = null);

/// <summary>
/// Resultado de la consulta. <c>Status</c> usa los valores de <c>PaymentStatus</c> del dominio (Pending, Processing,
/// Confirmed o Failed). <c>ExternalReference</c> es la que informa la pasarela.
/// </summary>
public sealed record PaymentVerification(
    string ExternalReference,
    string Status,
    decimal Amount,
    string Currency,
    string? TransactionId);

/// <summary>Notificación recibida: cuerpo crudo exacto y cabeceras (nombres sin distinguir mayúsculas).</summary>
public sealed record PaymentNotification(
    string RawBody,
    IReadOnlyDictionary<string, string> Headers,
    string? ContentType = null);

/// <summary>
/// Pago al que se refiere una notificación auténtica. <c>Acknowledgement</c> es el cuerpo que exige la pasarela en la
/// respuesta (nulo = 200 sin cuerpo). <c>SignedStatus</c> solo lo informa una pasarela sin consulta de estado
/// (botón de Banco de Chile mientras no se configure la consulta): es el estado firmado de la propia notificación y se
/// compara igual que una consulta. Las pasarelas con consulta lo dejan nulo y el estado se consulta siempre.
/// </summary>
public sealed record PaymentNotificationInfo(
    string? ProviderReference,
    string? ExternalReference,
    string? Acknowledgement = null,
    PaymentVerification? SignedStatus = null);
