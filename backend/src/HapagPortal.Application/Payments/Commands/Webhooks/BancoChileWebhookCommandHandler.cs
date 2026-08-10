namespace HapagPortal.Application.Payments.Commands.Webhooks;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class BancoChileWebhookCommandHandler(
    IApplicationDbContext dbContext,
    IWebhookAuthenticator webhookAuthenticator)
    : ICommandHandler<BancoChileWebhookCommand>
{
    private const string BancoChileStatusApproved = "approved";
    private const string BancoChileStatusRejected = "rejected";
    private const string BancoChileStatusPending = "pending";
    private const decimal AmountTolerance = 0.01m;

    public async Task<Result> Handle(
        BancoChileWebhookCommand request,
        CancellationToken cancellationToken)
    {
        if (!webhookAuthenticator.WebhooksEnabled)
            return Result.Failure(new Error("Webhook.Disabled", "Payment webhooks are disabled."));

        if (!webhookAuthenticator.IsValid("BancoChile", request.Secret) ||
            string.IsNullOrWhiteSpace(request.TransactionId))
        {
            return Result.Failure(Error.Unauthorized);
        }

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.ExternalReference == request.ExternalReference, cancellationToken);

        // Ack silencioso si no existe: no revela si la referencia es valida.
        if (payment is null)
            return Result.Success();

        if (payment.Status is PaymentStatus.Confirmed or PaymentStatus.Cancelled)
            return Result.Success();

        // El monto notificado debe coincidir con el total del pago; si no, no se confirma.
        if (request.Amount is null ||
            Math.Abs(request.Amount.Value - payment.TotalAmount) > AmountTolerance)
        {
            return Result.Failure(DomainErrors.Payment.InvalidAmount);
        }

        payment.Status = request.Status switch
        {
            BancoChileStatusApproved => PaymentStatus.Confirmed,
            BancoChileStatusRejected => PaymentStatus.Failed,
            BancoChileStatusPending => PaymentStatus.Processing,
            _ => payment.Status
        };

        if (payment.Status == PaymentStatus.Confirmed)
        {
            payment.ConfirmedAt = DateTime.UtcNow;
            payment.ConfirmedBy = "BANCOCHILE_WEBHOOK";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
