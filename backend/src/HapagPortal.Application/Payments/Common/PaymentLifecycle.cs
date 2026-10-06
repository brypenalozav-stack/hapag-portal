namespace HapagPortal.Application.Payments.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Quién ejecuta una transición (NF-14): usuario (correo e id), proveedor o sistema.</summary>
public sealed record PaymentActor(string Name, Guid? UserId)
{
    public static readonly PaymentActor System = new("SYSTEM", null);

    public static PaymentActor From(ICurrentUserService currentUserService) =>
        new(currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "system", currentUserService.UserId);
}

/// <summary>
/// Ciclo de vida de un pago (NF-02, NF-03): cada cambio de estado pasa por el modelo de estados y queda
/// en el historial con fecha, hora y actor. La confirmación asigna el comprobante, saca los ítems del carro
/// y encola los pasos posteriores (liberación y aviso) en la cola recuperable: si un paso falla, el pago
/// sigue confirmado. Fallo o anulación devuelven los ítems al carro. No guarda: lo hace el llamador.
/// </summary>
public static class PaymentLifecycle
{
    public const int DefaultMaxAttempts = 5;

    /// <summary>Registra el estado inicial del pago en el historial.</summary>
    public static void Created(IApplicationDbContext dbContext, Payment payment, PaymentActor actor, DateTime now)
    {
        dbContext.PaymentStatusChanges.Add(new PaymentStatusChange
        {
            PaymentId = payment.Id,
            FromStatus = null,
            ToStatus = payment.Status,
            ChangedAt = now,
            ChangedBy = actor.Name,
            ChangedByUserId = actor.UserId
        });
        payment.StatusChangedAt = now;
    }

    /// <summary>Transición validada por <see cref="PaymentStateMachine"/>. El mismo estado no es transición.</summary>
    public static Result Transition(
        IApplicationDbContext dbContext,
        Payment payment,
        string to,
        PaymentActor actor,
        string? reason,
        DateTime now)
    {
        if (payment.Status == to)
            return Result.Success();

        if (!PaymentStateMachine.CanTransition(payment.Status, to))
            return Result.Failure(DomainErrors.PaymentFlow.InvalidTransition(payment.Status, to));

        dbContext.PaymentStatusChanges.Add(new PaymentStatusChange
        {
            PaymentId = payment.Id,
            FromStatus = payment.Status,
            ToStatus = to,
            ChangedAt = now,
            ChangedBy = actor.Name,
            ChangedByUserId = actor.UserId,
            Reason = reason
        });

        payment.Status = to;
        payment.StatusChangedAt = now;
        return Result.Success();
    }

    /// <summary>Confirmación (webhook, verificación o Finanzas). Idempotente sobre un pago ya confirmado.</summary>
    public static async Task<Result> ConfirmAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        PaymentActor actor,
        string? transactionId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Confirmed)
            return Result.Success();

        var transition = Transition(dbContext, payment, PaymentStatus.Confirmed, actor, null, now);
        if (transition.IsFailure)
            return transition;

        payment.ConfirmedAt = now;
        payment.ConfirmedBy = actor.Name;
        payment.FailureReason = null;
        payment.ProviderTransactionId ??= transactionId;
        payment.ReceiptNumber ??= NewNumber(DocumentPrefixes.Receipt, now);

        // Pagados: dejan el carro.
        var items = await dbContext.CartItems
            .Where(i => i.LockedByPaymentId == payment.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in items)
            dbContext.CartItems.Remove(item);

        // NF-03: la liberación y el aviso van a la cola recuperable, fuera de la confirmación.
        Enqueue(dbContext, payment.Id, PaymentOutboxJobTypes.Release, now);
        Enqueue(dbContext, payment.Id, PaymentOutboxJobTypes.Notify, now);

        return Result.Success();
    }

    /// <summary>Rechazo o indisponibilidad de la plataforma (NF-12): sin cobro; los ítems vuelven al carro.</summary>
    public static async Task<Result> FailAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        PaymentActor actor,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var transition = Transition(dbContext, payment, PaymentStatus.Failed, actor, reason, now);
        if (transition.IsFailure)
            return transition;

        payment.FailureReason = reason;
        await UnlockAsync(dbContext, payment.Id, cancellationToken);
        return Result.Success();
    }

    /// <summary>Anulación con trazabilidad (M5-02): usuario, rol, fecha y motivo; los ítems vuelven al carro.</summary>
    public static async Task<Result> CancelAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        PaymentActor actor,
        string role,
        string? reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var transition = Transition(dbContext, payment, PaymentStatus.Cancelled, actor, reason, now);
        if (transition.IsFailure)
            return transition;

        payment.CancelledAt = now;
        payment.CancelledBy = actor.Name;
        payment.CancelledByUserId = actor.UserId;
        payment.CancelledByRole = role;
        payment.CancellationReason = reason;

        await UnlockAsync(dbContext, payment.Id, cancellationToken);
        return Result.Success();
    }

    public static PaymentOutboxMessage Enqueue(IApplicationDbContext dbContext, Guid paymentId, string jobType, DateTime now)
    {
        var message = new PaymentOutboxMessage
        {
            PaymentId = paymentId,
            JobType = jobType,
            Status = PaymentOutboxStatus.Pending,
            MaxAttempts = DefaultMaxAttempts,
            NextAttemptAt = now,
            CreatedAt = now
        };

        dbContext.PaymentOutboxMessages.Add(message);
        return message;
    }

    public static string NewNumber(string prefix, DateTime now) =>
        $"{prefix}{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static async Task UnlockAsync(IApplicationDbContext dbContext, Guid paymentId, CancellationToken cancellationToken)
    {
        var items = await dbContext.CartItems
            .Where(i => i.LockedByPaymentId == paymentId)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            item.LockedByPaymentId = null;
    }
}
