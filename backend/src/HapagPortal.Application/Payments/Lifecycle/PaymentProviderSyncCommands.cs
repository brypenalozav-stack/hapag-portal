namespace HapagPortal.Application.Payments.Lifecycle;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Consulta a la pasarela el estado de un pago en curso cuando el pagador vuelve al portal (página de resultado). La
/// vuelta del pagador nunca confirma por sí sola: el estado sale de la pasarela (<see cref="PaymentStatusSync"/>).
/// Devuelve el estado del pago, como <see cref="GetPaymentStatusQuery"/>.
/// </summary>
public sealed record VerifyPaymentCommand(Guid PaymentId) : ICommand<PaymentStatusDto>;

/// <summary>
/// Conciliación periódica (proceso en segundo plano): consulta a la pasarela los pagos en línea en curso con al menos
/// <paramref name="MinAgeMinutes"/> minutos sin consultar y hasta <paramref name="MaxAgeHours"/> horas de antigüedad.
/// Cubre las notificaciones perdidas (Getnet notifica una sola vez). Devuelve cuántos pagos cambiaron de estado.
/// </summary>
public sealed record ReconcileOnlinePaymentsCommand(int MinAgeMinutes, int MaxAgeHours, int BatchSize) : ICommand<int>;

public sealed class VerifyPaymentCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    IPaymentProviderResolver providerResolver)
    : ICommandHandler<VerifyPaymentCommand, PaymentStatusDto>
{
    /// <summary>Entre dos consultas del mismo pago desde el retorno, para no saturar la pasarela con recargas.</summary>
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);

    private static readonly PaymentActor ReturnActor = new("PAYER_RETURN_CHECK", null);

    public async Task<Result<PaymentStatusDto>> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        var loaded = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: false, tracking: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<PaymentStatusDto>.Failure(loaded.Error);

        var payment = loaded.Value;
        if (OnlinePaymentChecks.ShouldQuery(payment, DateTime.UtcNow, MinInterval) &&
            providerResolver.Resolve(payment.ProviderKey!) is { } provider &&
            (provider.VerifiesNotifications || provider is ISimulatedPaymentProvider))
        {
            // La pasarela simulada (modo de prueba) informa el resultado elegido en el simulador de pago.
            // Si la pasarela no responde o no coincide, el pago queda como estaba: la conciliación reintenta.
            await PaymentStatusSync.SyncAsync(dbContext, payment, provider, ReturnActor, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<PaymentStatusDto>.Success(await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken));
    }
}

public sealed class ReconcileOnlinePaymentsCommandHandler(
    IApplicationDbContext dbContext,
    IPaymentProviderResolver providerResolver)
    : ICommandHandler<ReconcileOnlinePaymentsCommand, int>
{
    private static readonly PaymentActor ReconciliationActor = new("PAYMENT_RECONCILIATION", null);

    public async Task<Result<int>> Handle(ReconcileOnlinePaymentsCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var checkedBefore = now.AddMinutes(-Math.Max(0, request.MinAgeMinutes));
        var createdAfter = now.AddHours(-Math.Max(1, request.MaxAgeHours));

        var candidates = await dbContext.Payments
            .Where(p => p.ProviderKey != null && p.ExternalReference != null
                && (p.Status == PaymentStatus.Processing || (p.Status == PaymentStatus.Pending && p.ProviderReference != null))
                && p.PaymentDate <= checkedBefore && p.PaymentDate >= createdAfter
                && (p.ProviderCheckedAt == null || p.ProviderCheckedAt <= checkedBefore))
            .OrderBy(p => p.ProviderCheckedAt ?? p.PaymentDate)
            .Take(Math.Clamp(request.BatchSize, 1, 500))
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var payment in candidates)
        {
            if (providerResolver.Resolve(payment.ProviderKey!) is not { VerifiesNotifications: true } provider)
                continue;

            var synced = await PaymentStatusSync.SyncAsync(dbContext, payment, provider, ReconciliationActor, cancellationToken);
            if (synced.IsSuccess && synced.Value.AppliedStatus is not null)
                changed++;

            // Se guarda cada pago: un fallo en uno no deshace la conciliación de los anteriores.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(changed);
    }
}

/// <summary>Cuándo vale la pena consultar a la pasarela el estado de un pago.</summary>
public static class OnlinePaymentChecks
{
    public static bool ShouldQuery(Payment payment, DateTime now, TimeSpan minInterval) =>
        !string.IsNullOrWhiteSpace(payment.ProviderKey) &&
        !string.IsNullOrWhiteSpace(payment.ExternalReference) &&
        (payment.Status == PaymentStatus.Processing || (payment.Status == PaymentStatus.Pending && payment.ProviderReference is not null)) &&
        (payment.ProviderCheckedAt is null || now - payment.ProviderCheckedAt.Value >= minInterval);
}
