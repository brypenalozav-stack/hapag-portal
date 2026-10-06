namespace HapagPortal.Application.Payments.PostProcessing;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Paso posterior a la confirmación de un pago (NF-03). Debe ser idempotente: se reintenta si falla. La
/// generación documental de la Ola E se agrega como otro paso registrado en el contenedor.
/// </summary>
public interface IPaymentPostStep
{
    string JobType { get; }

    Task ExecuteAsync(Payment payment, IReadOnlyList<PaymentDetail> details, DateTime now, CancellationToken cancellationToken);
}

/// <summary>
/// Liberación: marca pagada la fuente de cada ítem (recargo, flete, línea de demurrage, cambio de almacén
/// o factura y, con ella, las líneas de demurrage facturadas) con la misma lógica para cualquier cliente,
/// también el de crédito (M5-07). Una fuente inexistente detiene el paso para resolución interna.
/// </summary>
public sealed class ReleasePaymentItemsStep(IApplicationDbContext dbContext) : IPaymentPostStep
{
    public string JobType => PaymentOutboxJobTypes.Release;

    public async Task ExecuteAsync(Payment payment, IReadOnlyList<PaymentDetail> details, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var detail in details.Where(d => d.ReleasedAt is null && d.ItemType is not null && d.SourceId is not null))
        {
            var id = detail.SourceId!.Value;

            switch (detail.ItemType)
            {
                case PayableItemTypes.LocalCharge:
                {
                    var charge = await dbContext.LocalCharges.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                        ?? throw Missing(detail);
                    charge.Status = ChargeStatus.Paid;
                    break;
                }

                case PayableItemTypes.Freight:
                {
                    var bl = await dbContext.BillsOfLading.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                        ?? throw Missing(detail);
                    bl.FreightPaidAt ??= now;
                    break;
                }

                case PayableItemTypes.Demurrage:
                {
                    var line = await dbContext.DemurrageCharges.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                        ?? throw Missing(detail);
                    line.Status = DemurrageChargeStatus.Paid;
                    break;
                }

                case PayableItemTypes.WarehouseChange:
                {
                    var change = await dbContext.WarehouseChanges.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
                        ?? throw Missing(detail);
                    change.Status = WarehouseChangeStatus.Completed;
                    change.CompletedAt ??= now;
                    break;
                }

                case PayableItemTypes.Invoice:
                {
                    var invoice = await dbContext.CustomerInvoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
                        ?? throw Missing(detail);
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidAt ??= now;
                    invoice.PaymentId = payment.Id;

                    // Una factura de demurrage paga sus líneas (M3-18).
                    if (invoice.BillOfLadingId is { } blId)
                    {
                        var lines = await dbContext.DemurrageCharges
                            .Where(d => d.BillOfLadingId == blId
                                && d.InvoiceNumber != null
                                && (d.InvoiceNumber == invoice.SourceNumber || d.InvoiceNumber == invoice.SiiNumber))
                            .ToListAsync(cancellationToken);
                        foreach (var line in lines)
                            line.Status = DemurrageChargeStatus.Paid;
                    }

                    break;
                }

                default:
                    throw new InvalidOperationException($"Unknown payable item type '{detail.ItemType}'.");
            }

            detail.ReleasedAt = now;
        }
    }

    private static InvalidOperationException Missing(PaymentDetail detail) =>
        new($"The {detail.ItemType} '{detail.SourceId}' of the payment no longer exists.");
}

/// <summary>Aviso al usuario que pagó (campana y correo).</summary>
public sealed class NotifyPaymentStep(INotificationPublisher notificationPublisher) : IPaymentPostStep
{
    public string JobType => PaymentOutboxJobTypes.Notify;

    public async Task ExecuteAsync(Payment payment, IReadOnlyList<PaymentDetail> details, DateTime now, CancellationToken cancellationToken)
    {
        if (payment.CreatedByUserId is null)
            return;

        await notificationPublisher.PublishAsync(
            new NotificationRequest(
                NotificationTypes.PaymentConfirmed,
                $"Pago confirmado {payment.PaymentNumber}",
                $"Pago {payment.PaymentNumber} confirmado por {payment.TotalAmount:N2} {payment.Currency}. Comprobante {payment.ReceiptNumber}.",
                UserId: payment.CreatedByUserId,
                DedupKey: $"payment-confirmed:{payment.Id}"),
            cancellationToken);
    }
}

/// <summary>
/// Ejecuta los pasos pendientes de la cola (NF-03). Un fallo no revierte el pago: se registra el error y
/// se reintenta con espera creciente (30 s, 1, 2, 4… min, tope 1 h); agotados los intentos queda
/// <c>Stuck</c> y aparece en la lista interna de operaciones detenidas.
/// </summary>
public sealed class PaymentPostProcessor(IApplicationDbContext dbContext, IEnumerable<IPaymentPostStep> steps)
{
    private const int MaxErrorLength = 1000;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromHours(1);

    public async Task<int> ProcessDueAsync(int batchSize, DateTime now, CancellationToken cancellationToken)
    {
        var due = await dbContext.PaymentOutboxMessages
            .Where(m => m.Status == PaymentOutboxStatus.Pending && m.NextAttemptAt <= now)
            .OrderBy(m => m.NextAttemptAt)
            .ThenBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in due)
            await ExecuteAsync(message, now, cancellationToken);

        return due.Count;
    }

    public async Task ExecuteAsync(PaymentOutboxMessage message, DateTime now, CancellationToken cancellationToken)
    {
        message.Attempts++;
        message.LastAttemptAt = now;

        try
        {
            var step = steps.FirstOrDefault(s => s.JobType == message.JobType)
                ?? throw new InvalidOperationException($"No post-payment step is registered for '{message.JobType}'.");
            var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == message.PaymentId, cancellationToken)
                ?? throw new InvalidOperationException($"The payment '{message.PaymentId}' does not exist.");
            var details = await dbContext.PaymentDetails
                .Where(d => d.PaymentId == payment.Id)
                .ToListAsync(cancellationToken);

            await step.ExecuteAsync(payment, details, now, cancellationToken);

            message.Status = PaymentOutboxStatus.Succeeded;
            message.CompletedAt = now;
            message.LastError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            message.LastError = ex.Message.Length > MaxErrorLength ? ex.Message[..MaxErrorLength] : ex.Message;

            if (message.Attempts >= message.MaxAttempts)
            {
                message.Status = PaymentOutboxStatus.Stuck;
            }
            else
            {
                var delay = TimeSpan.FromTicks(Math.Min(
                    MaxDelay.Ticks,
                    BaseDelay.Ticks * (long)Math.Pow(2, message.Attempts - 1)));
                message.NextAttemptAt = now + delay;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static PaymentOperationDto ToDto(PaymentOutboxMessage m, string paymentNumber) => new(
        m.Id, m.PaymentId, paymentNumber, m.JobType, m.Status, m.Attempts, m.MaxAttempts, m.NextAttemptAt, m.LastAttemptAt,
        m.LastError, m.CreatedAt, m.CompletedAt);
}
