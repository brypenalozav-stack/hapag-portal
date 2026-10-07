using HapagPortal.Domain.Constants;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Pasarela simulada (modo de prueba, <c>Integrations:&lt;Proveedor&gt;:Mode=Dummy</c>). Solo un adaptador que la
/// implementa admite el simulador de pago (<c>POST /api/v1/payments/simulator/{referencia}</c>): con una pasarela Real
/// el simulador no existe (404). Ver docs/integraciones/pasarelas-pago.md, «Simulador en modo de prueba».
/// </summary>
public interface ISimulatedPaymentProvider : IPaymentProvider;

/// <summary>
/// Resultado elegido por el usuario en la página del simulador, por referencia del portal. Es único para todos los
/// adaptadores simulados (singleton). Vive en memoria: tras un reinicio de la API se pierde, pero el resultado ya
/// quedó aplicado al pago al elegirlo; sin resultado, la pasarela simulada informa el pago como en curso.
/// </summary>
public interface IPaymentSimulatorStore
{
    void Record(string externalReference, string outcome);

    string? Outcome(string externalReference);
}

/// <summary>Resultados que el usuario elige en el simulador.</summary>
public static class PaymentSimulatorOutcomes
{
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Pending = "pending";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All = [Approved, Rejected, Pending, Cancelled];

    /// <summary>
    /// Estado que informa la pasarela simulada: aprobado → Confirmed; rechazado o cancelado → Failed (el pago no se
    /// cobró y sus ítems vuelven al carro); pendiente o sin elegir → Processing (el pagador aún no paga).
    /// </summary>
    public static string StatusFor(string? outcome) => outcome?.Trim().ToLowerInvariant() switch
    {
        Approved => PaymentStatus.Confirmed,
        Rejected or Cancelled => PaymentStatus.Failed,
        _ => PaymentStatus.Processing,
    };
}
