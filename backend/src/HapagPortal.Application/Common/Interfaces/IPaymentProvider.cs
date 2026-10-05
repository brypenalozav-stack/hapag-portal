using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Proveedor de pago en línea (CT-KHIPU, CT-BCH, CT-SANT, CT-BCI). Se registra como servicio con clave
/// (<c>Khipu</c>, <c>BancoChile</c>, <c>Santander</c>, <c>Bci</c>) y se resuelve con <c>[FromKeyedServices]</c>.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Clave con la que el proveedor está registrado.</summary>
    string ProviderCode { get; }

    /// <summary>
    /// Indica si <see cref="VerifyNotificationAsync"/> consulta al proveedor. Dummy = false (el webhook
    /// toma el estado del cuerpo, como hoy); Real = true.
    /// </summary>
    bool VerifiesNotifications { get; }

    Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Consulta al proveedor el estado de un pago notificado por webhook.</summary>
    Task<Result<PaymentVerification>> VerifyNotificationAsync(
        string notificationToken,
        string externalReference,
        CancellationToken cancellationToken = default);
}

/// <summary>Solicitud de pago. <c>ExternalReference</c> es la referencia del portal.</summary>
public sealed record PaymentInitiationRequest(
    string ExternalReference,
    decimal Amount,
    string Currency,
    string Subject,
    string ReturnUrl,
    string NotifyUrl,
    string? PayerTaxId);

/// <summary>Pago creado en el proveedor: referencia del proveedor y URL a la que se redirige al pagador.</summary>
public sealed record PaymentInitiation(
    string ProviderReference,
    string ExternalReference,
    string Status,
    string RedirectUrl);

/// <summary>Resultado de la verificación. <c>Status</c> usa los valores de <c>PaymentStatus</c> del dominio.</summary>
public sealed record PaymentVerification(
    string ExternalReference,
    string Status,
    decimal Amount,
    string Currency,
    string? TransactionId);
