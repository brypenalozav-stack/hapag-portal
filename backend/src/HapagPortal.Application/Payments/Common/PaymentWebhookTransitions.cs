namespace HapagPortal.Application.Payments.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>
/// Aplica el estado notificado por una plataforma de pago. Una notificación que el modelo de estados no
/// admite (p. ej. "pendiente" sobre un pago fallido) se reconoce sin cambios: la plataforma no reintenta y
/// el pago conserva un estado único (NF-02).
/// </summary>
public static class PaymentWebhookTransitions
{
    public static async Task ApplyAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        string newStatus,
        PaymentActor actor,
        string? transactionId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        switch (newStatus)
        {
            case PaymentStatus.Confirmed:
                await PaymentLifecycle.ConfirmAsync(dbContext, payment, actor, transactionId, now, cancellationToken);
                break;

            case PaymentStatus.Failed:
                await PaymentLifecycle.FailAsync(dbContext, payment, actor, PaymentFailureReasons.ProviderRejected, now, cancellationToken);
                break;

            default:
                PaymentLifecycle.Transition(dbContext, payment, newStatus, actor, null, now);
                break;
        }
    }
}
