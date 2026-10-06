namespace HapagPortal.Application.Payments.Commands.Webhooks;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public sealed class KhipuWebhookCommandHandler(
    IApplicationDbContext dbContext,
    IWebhookAuthenticator webhookAuthenticator,
    [FromKeyedServices("Khipu")] IPaymentProvider paymentProvider)
    : ICommandHandler<KhipuWebhookCommand>
{
    private const string KhipuStatusDone = "done";
    private const string KhipuStatusRejected = "rejected";
    private const string KhipuStatusPending = "pending";
    private const decimal AmountTolerance = 0.01m;
    private static readonly PaymentActor WebhookActor = new("KHIPU_WEBHOOK", null);

    public async Task<Result> Handle(
        KhipuWebhookCommand request,
        CancellationToken cancellationToken)
    {
        if (!webhookAuthenticator.WebhooksEnabled)
            return Result.Failure(new Error("Webhook.Disabled", "Payment webhooks are disabled."));

        // Autenticacion por secreto compartido (fail-closed). En modo Real, ademas, el pago se
        // verifica contra Khipu (VerifiesNotifications).
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

        string? newStatus;
        string? transactionId = null;

        if (paymentProvider.VerifiesNotifications)
        {
            var verified = await VerifiedStatusAsync(request, payment, cancellationToken);
            if (verified.IsFailure)
                return Result.Failure(verified.Error);

            newStatus = verified.Value.Status is PaymentStatus.Confirmed or PaymentStatus.Failed or PaymentStatus.Processing
                ? verified.Value.Status
                : null;
            transactionId = verified.Value.TransactionId;
        }
        else
        {
            // Modo Dummy: el estado sale del cuerpo, como antes de la Fase 6c.
            newStatus = request.Status switch
            {
                KhipuStatusDone => PaymentStatus.Confirmed,
                KhipuStatusRejected => PaymentStatus.Failed,
                KhipuStatusPending => PaymentStatus.Processing,
                _ => null
            };
        }

        // Ola D: la transición pasa por el ciclo de vida (historial NF-02, liberación NF-03). Un pago ya
        // cobrado se reconoce aunque haya un bloqueo de pagos vigente (M8-07).
        if (newStatus is not null)
            await PaymentWebhookTransitions.ApplyAsync(dbContext, payment, newStatus, WebhookActor, transactionId, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Consulta el pago a Khipu por el token de la notificacion. Si la consulta falla, o la referencia o
    /// el monto no coinciden con el pago del portal, la notificacion se rechaza sin cambios.
    /// </summary>
    private async Task<Result<PaymentVerification>> VerifiedStatusAsync(
        KhipuWebhookCommand request,
        Payment payment,
        CancellationToken cancellationToken)
    {
        var verification = await paymentProvider.VerifyNotificationAsync(
            request.NotificationToken, request.ExternalReference, cancellationToken);

        if (verification.IsFailure)
            return Result<PaymentVerification>.Failure(Error.Unauthorized);

        // El monto cobrado es el total del pago (el que se envia a la pasarela al crearlo).
        var matches =
            string.Equals(verification.Value.ExternalReference, request.ExternalReference, StringComparison.Ordinal) &&
            Math.Abs(verification.Value.Amount - payment.TotalAmount) <= AmountTolerance;

        return matches
            ? Result<PaymentVerification>.Success(verification.Value)
            : Result<PaymentVerification>.Failure(Error.Unauthorized);
    }
}
