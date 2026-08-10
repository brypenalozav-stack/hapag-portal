namespace HapagPortal.Application.Payments.Commands.Webhooks;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class KhipuWebhookCommandHandler(
    IApplicationDbContext dbContext,
    IWebhookAuthenticator webhookAuthenticator)
    : ICommandHandler<KhipuWebhookCommand>
{
    private const string KhipuStatusDone = "done";
    private const string KhipuStatusRejected = "rejected";
    private const string KhipuStatusPending = "pending";

    public async Task<Result> Handle(
        KhipuWebhookCommand request,
        CancellationToken cancellationToken)
    {
        if (!webhookAuthenticator.WebhooksEnabled)
            return Result.Failure(new Error("Webhook.Disabled", "Payment webhooks are disabled."));

        // Autenticacion por secreto compartido (fail-closed). NOTA: la integracion real de
        // Khipu exige validar NotificationToken contra su API; esto es la capa intermedia.
        if (!webhookAuthenticator.IsValid("Khipu", request.Secret) ||
            string.IsNullOrWhiteSpace(request.NotificationToken))
        {
            return Result.Failure(Error.Unauthorized);
        }

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.ExternalReference == request.ExternalReference, cancellationToken);

        // Ack silencioso si no existe: no revela si la referencia es valida.
        if (payment is null)
            return Result.Success();

        // Estado terminal -> idempotente, sin efectos.
        if (payment.Status is PaymentStatus.Confirmed or PaymentStatus.Cancelled)
            return Result.Success();

        payment.Status = request.Status switch
        {
            KhipuStatusDone => PaymentStatus.Confirmed,
            KhipuStatusRejected => PaymentStatus.Failed,
            KhipuStatusPending => PaymentStatus.Processing,
            _ => payment.Status
        };

        if (payment.Status == PaymentStatus.Confirmed)
        {
            payment.ConfirmedAt = DateTime.UtcNow;
            payment.ConfirmedBy = "KHIPU_WEBHOOK";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
