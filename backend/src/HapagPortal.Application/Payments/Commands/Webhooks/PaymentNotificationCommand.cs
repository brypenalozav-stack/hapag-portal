namespace HapagPortal.Application.Payments.Commands.Webhooks;

using System.Globalization;
using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Notificación de una pasarela de pago (<c>POST /api/v1/payments/webhook/{pasarela}</c>) con el cuerpo crudo y las
/// cabeceras tal como llegaron: la firma se verifica sobre esos bytes, antes de interpretar nada.
/// </summary>
public sealed record PaymentNotificationCommand(
    string ProviderKey,
    string RawBody,
    IReadOnlyDictionary<string, string> Headers,
    string? ContentType = null) : ICommand<PaymentNotificationAck>;

/// <summary>Respuesta a la pasarela: cuerpo que exige (nulo = 200 sin cuerpo).</summary>
public sealed record PaymentNotificationAck(string? Body);

/// <summary>
/// Procesa la notificación de una pasarela (M5-03, NF-02, NF-12), idempotente:
/// <list type="bullet">
/// <item>Real: la pasarela autentica la notificación (firma sobre el cuerpo crudo); luego el estado se consulta a la
/// pasarela y se compara referencia, monto y moneda (<see cref="PaymentStatusSync"/>). El cuerpo no decide nada.</item>
/// <item>Dummy (desarrollo y pruebas): notificación simulada con secreto compartido en <c>X-Webhook-Secret</c>
/// (<c>Payments:Webhooks:&lt;Pasarela&gt;:Secret</c>) y JSON <c>{ externalReference, status, transactionId?, amount? }</c>.</item>
/// </list>
/// Con <c>Payments:Webhooks:Enabled=false</c> toda notificación se rechaza. Una referencia desconocida se reconoce sin
/// revelar nada; un pago confirmado o anulado no cambia.
/// </summary>
public sealed class PaymentNotificationCommandHandler(
    IApplicationDbContext dbContext,
    IWebhookAuthenticator webhookAuthenticator,
    IPaymentProviderResolver providerResolver)
    : ICommandHandler<PaymentNotificationCommand, PaymentNotificationAck>
{
    public const string SimulatedSecretHeader = "X-Webhook-Secret";
    private static readonly PaymentNotificationAck EmptyAck = new(null);

    public async Task<Result<PaymentNotificationAck>> Handle(PaymentNotificationCommand request, CancellationToken cancellationToken)
    {
        if (!webhookAuthenticator.WebhooksEnabled)
            return Result<PaymentNotificationAck>.Failure(new Error("Webhook.Disabled", "Payment webhooks are disabled."));

        var provider = providerResolver.Resolve(request.ProviderKey);
        if (provider is null)
            return Result<PaymentNotificationAck>.Failure(Error.Unauthorized);

        var actor = new PaymentActor($"{request.ProviderKey.ToUpperInvariant()}_WEBHOOK", null);

        return provider.VerifiesNotifications
            ? await HandleRealAsync(request, provider, actor, cancellationToken)
            : await HandleSimulatedAsync(request, actor, cancellationToken);
    }

    private async Task<Result<PaymentNotificationAck>> HandleRealAsync(
        PaymentNotificationCommand request,
        IPaymentProvider provider,
        PaymentActor actor,
        CancellationToken cancellationToken)
    {
        var read = await provider.ReadNotificationAsync(
            new PaymentNotification(request.RawBody, request.Headers, request.ContentType), cancellationToken);
        if (read.IsFailure)
            return Result<PaymentNotificationAck>.Failure(Error.Unauthorized);

        var ack = new PaymentNotificationAck(read.Value.Acknowledgement);
        var payment = await FindAsync(request.ProviderKey, read.Value.ExternalReference, read.Value.ProviderReference, cancellationToken);

        // Ack silencioso si no existe (no revela si la referencia es válida); estado terminal: idempotente.
        if (payment is null || PaymentStateMachine.IsTerminal(payment.Status))
            return Result<PaymentNotificationAck>.Success(ack);

        var synced = await PaymentStatusSync.SyncAsync(dbContext, payment, provider, actor, cancellationToken, read.Value.SignedStatus);
        if (synced.IsFailure)
            return Result<PaymentNotificationAck>.Failure(synced.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentNotificationAck>.Success(ack);
    }

    private async Task<Result<PaymentNotificationAck>> HandleSimulatedAsync(
        PaymentNotificationCommand request,
        PaymentActor actor,
        CancellationToken cancellationToken)
    {
        request.Headers.TryGetValue(SimulatedSecretHeader, out var secret);
        if (!webhookAuthenticator.IsValid(request.ProviderKey, secret))
            return Result<PaymentNotificationAck>.Failure(Error.Unauthorized);

        var body = SimulatedNotification.Parse(request.RawBody);
        if (body is null || string.IsNullOrWhiteSpace(body.ExternalReference))
            return Result<PaymentNotificationAck>.Failure(Error.Unauthorized);

        var payment = await FindAsync(request.ProviderKey, body.ExternalReference, null, cancellationToken);
        if (payment is null || PaymentStateMachine.IsTerminal(payment.Status))
            return Result<PaymentNotificationAck>.Success(EmptyAck);

        if (body.Amount is { } amount && Math.Abs(amount - payment.TotalAmount) > PaymentStatusSync.AmountTolerance)
            return Result<PaymentNotificationAck>.Failure(Domain.Errors.DomainErrors.Payment.InvalidAmount);

        var newStatus = body.Status?.Trim().ToLowerInvariant() switch
        {
            "done" or "approved" or "paid" => PaymentStatus.Confirmed,
            "rejected" or "failed" => PaymentStatus.Failed,
            "pending" => PaymentStatus.Processing,
            _ => null
        };

        if (newStatus is not null && PaymentStateMachine.CanTransition(payment.Status, newStatus))
            await PaymentWebhookTransitions.ApplyAsync(dbContext, payment, newStatus, actor, body.TransactionId, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentNotificationAck>.Success(EmptyAck);
    }

    /// <summary>Pago en línea de la pasarela por la referencia del portal o, si no viene, por la de la pasarela.</summary>
    private async Task<Payment?> FindAsync(string providerKey, string? externalReference, string? providerReference, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(externalReference))
        {
            var byReference = await dbContext.Payments
                .FirstOrDefaultAsync(p => p.ExternalReference == externalReference, cancellationToken);

            // Un pago de otra pasarela no se toca desde esta notificación.
            return byReference is not null && (byReference.ProviderKey is null || string.Equals(byReference.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase))
                ? byReference
                : null;
        }

        if (string.IsNullOrWhiteSpace(providerReference))
            return null;

        return await dbContext.Payments
            .FirstOrDefaultAsync(p => p.ProviderKey == providerKey && p.ProviderReference == providerReference, cancellationToken);
    }

    private sealed record SimulatedNotification(string? ExternalReference, string? Status, string? TransactionId, decimal? Amount)
    {
        public static SimulatedNotification? Parse(string rawBody)
        {
            try
            {
                using var document = JsonDocument.Parse(rawBody);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                return new SimulatedNotification(
                    Text(root, "externalReference"),
                    Text(root, "status"),
                    Text(root, "transactionId") ?? Text(root, "notificationToken"),
                    Number(root, "amount"));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? Text(JsonElement root, string name) =>
            root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static decimal? Number(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var value))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
                JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null
            };
        }
    }
}
