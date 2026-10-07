using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Payments;

/// <summary>
/// Modelo de estados de un pago (NF-02). Ninguna operación queda fuera de él: toda transición que no
/// figure aquí se rechaza.
/// <list type="bullet">
/// <item>Pending → Processing (iniciado en la plataforma), PendingVerification (boleta de depósito emitida),
/// Confirmed (confirmación de Finanzas), Failed (la plataforma no respondió o rechazó) o Cancelled.</item>
/// <item>Processing → Confirmed, Failed o Cancelled (solo Finanzas).</item>
/// <item>PendingVerification → Confirmed o Cancelled (solo Finanzas, M5-02).</item>
/// <item>Failed → Confirmed: la plataforma confirma tarde un cobro que sí ocurrió (NF-12, el abono no se pierde).</item>
/// <item>Confirmed y Cancelled son terminales.</item>
/// </list>
/// </summary>
public static class PaymentStateMachine
{
    private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.Ordinal)
    {
        [PaymentStatus.Pending] =
        [
            PaymentStatus.Processing, PaymentStatus.PendingVerification, PaymentStatus.Confirmed,
            PaymentStatus.Failed, PaymentStatus.Cancelled
        ],
        [PaymentStatus.Processing] = [PaymentStatus.Confirmed, PaymentStatus.Failed, PaymentStatus.Cancelled],
        [PaymentStatus.PendingVerification] = [PaymentStatus.Confirmed, PaymentStatus.Cancelled],
        [PaymentStatus.Failed] = [PaymentStatus.Confirmed],
        [PaymentStatus.Confirmed] = [],
        [PaymentStatus.Cancelled] = [],
    };

    public static bool CanTransition(string from, string to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to, StringComparer.Ordinal);

    public static bool IsTerminal(string status) =>
        status is PaymentStatus.Confirmed or PaymentStatus.Cancelled;

    public static bool IsInFlight(string status) => PaymentStatus.InFlight.Contains(status, StringComparer.Ordinal);
}

/// <summary>
/// Reglas de anulación de boletas y pagos (resultado de la evaluación de M5-02):
/// <list type="number">
/// <item>El cliente solo anula mientras el pago está <c>Pending</c>: la boleta de depósito todavía no se
/// emitió o el pago en línea aún no se envió a la plataforma.</item>
/// <item>Desde la emisión de la boleta de depósito (<c>PendingVerification</c>) el cliente ya no puede
/// eliminarla: transcurre el tiempo hasta el abono y la verificación de Finanzas.</item>
/// <item>Un pago en línea en curso (<c>Processing</c>) no se anula desde el portal del cliente: lo resuelve
/// la plataforma (NF-12) o Finanzas.</item>
/// <item>Finanzas puede anular un pago en curso o una boleta emitida, con motivo obligatorio.</item>
/// <item>Toda anulación registra usuario, rol (cliente o Finanzas), fecha, motivo y la transición (NF-02, NF-14).</item>
/// </list>
/// </summary>
public static class ReceiptCancellationPolicy
{
    public const string SlipIssued = "SLIP_ISSUED";
    public const string InProgress = "IN_PROGRESS";
    public const string Final = "FINAL";

    /// <summary>Nulo si el cliente puede anular; si no, el motivo.</summary>
    public static string? ClientDenialReason(string status) => status switch
    {
        PaymentStatus.Pending => null,
        PaymentStatus.PendingVerification => SlipIssued,
        PaymentStatus.Processing => InProgress,
        _ => Final
    };

    public static bool FinanceCanCancel(string status) =>
        status is PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.PendingVerification;
}
