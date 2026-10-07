namespace HapagPortal.Application.Payments.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;

/// <summary>Resultado de una sincronización: el estado aplicado (o nulo si no hubo cambio) y si la consulta se hizo.</summary>
public sealed record PaymentSyncOutcome(bool Queried, string? AppliedStatus);

/// <summary>
/// Sincroniza un pago en línea con su pasarela (M5-03, NF-02, NF-12). Es el único camino por el que una pasarela Real
/// confirma o rechaza un pago: lo usan la notificación (webhook), el retorno del pagador y la conciliación periódica.
/// <list type="bullet">
/// <item>Consulta el estado a la pasarela; nunca confía en el cuerpo de la notificación ni en la URL de retorno.</item>
/// <item>Compara la referencia del portal, el monto (tolerancia 0,01) y la moneda; si no coinciden, no cambia nada.</item>
/// <item>Idempotente: un pago confirmado o anulado no cambia; repetir la sincronización no repite efectos.</item>
/// </list>
/// No guarda: lo hace el llamador.
/// </summary>
public static class PaymentStatusSync
{
    public const decimal AmountTolerance = 0.01m;

    public static async Task<Result<PaymentSyncOutcome>> SyncAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        IPaymentProvider provider,
        PaymentActor actor,
        CancellationToken cancellationToken,
        PaymentVerification? signedStatus = null)
    {
        if (PaymentStateMachine.IsTerminal(payment.Status) || string.IsNullOrWhiteSpace(payment.ExternalReference))
            return Result<PaymentSyncOutcome>.Success(new PaymentSyncOutcome(false, null));

        // Solo una pasarela sin consulta de estado entrega el estado firmado de la notificación; el resto se consulta.
        var verification = signedStatus is not null
            ? Result<PaymentVerification>.Success(signedStatus)
            : await provider.GetStatusAsync(
                new PaymentStatusRequest(payment.ProviderReference, payment.ExternalReference, payment.TotalAmount, payment.Currency),
                cancellationToken);

        payment.ProviderCheckedAt = DateTime.UtcNow;

        if (verification.IsFailure)
            return Result<PaymentSyncOutcome>.Failure(verification.Error);

        if (!Matches(payment, verification.Value))
            return Result<PaymentSyncOutcome>.Failure(DomainErrors.Payment.VerificationMismatch);

        var status = verification.Value.Status;

        // Pending de la pasarela = el pagador aún no paga: el pago del portal sigue en curso (Processing).
        if (status is not (PaymentStatus.Confirmed or PaymentStatus.Failed or PaymentStatus.Processing) ||
            status == payment.Status ||
            !PaymentStateMachine.CanTransition(payment.Status, status))
        {
            return Result<PaymentSyncOutcome>.Success(new PaymentSyncOutcome(true, null));
        }

        await PaymentWebhookTransitions.ApplyAsync(
            dbContext, payment, status, actor, verification.Value.TransactionId, cancellationToken);

        return Result<PaymentSyncOutcome>.Success(new PaymentSyncOutcome(true, status));
    }

    /// <summary>La pasarela informa la misma referencia, el mismo monto y la misma moneda que el pago del portal.</summary>
    public static bool Matches(Payment payment, PaymentVerification verification) =>
        string.Equals(verification.ExternalReference, payment.ExternalReference, StringComparison.Ordinal) &&
        Math.Abs(verification.Amount - payment.TotalAmount) <= AmountTolerance &&
        string.Equals(verification.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase);
}
