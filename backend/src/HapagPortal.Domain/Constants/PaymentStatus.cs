namespace HapagPortal.Domain.Constants;

/// <summary>
/// Estado único de un pago (NF-02). Toda transición queda registrada con fecha y hora en
/// <c>PaymentStatusChange</c> y debe ser una de las permitidas por <c>PaymentStateMachine</c>:
/// <list type="bullet">
/// <item><see cref="Pending"/>: creado en el portal; aún no se envió a la plataforma de pago o es una boleta de depósito sin emitir.</item>
/// <item><see cref="Processing"/>: iniciado en la plataforma de pago; se espera su confirmación.</item>
/// <item><see cref="PendingVerification"/>: boleta de depósito emitida; Finanzas verifica el abono (M5-02).</item>
/// <item><see cref="Confirmed"/>: pago confirmado por la plataforma o por Finanzas. Terminal.</item>
/// <item><see cref="Failed"/>: la plataforma rechazó el pago o no respondió (NF-12); no hubo cobro.</item>
/// <item><see cref="Cancelled"/>: anulado por el cliente antes de emitirse o por Finanzas. Terminal.</item>
/// </list>
/// </summary>
public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string Processing = "Processing";
    public const string PendingVerification = "PendingVerification";

    public static readonly string[] All = [Pending, Processing, PendingVerification, Confirmed, Failed, Cancelled];

    /// <summary>Pagos en curso: sus ítems no pueden agregarse a otro pago (NF-01).</summary>
    public static readonly string[] InFlight = [Pending, Processing, PendingVerification];
}
