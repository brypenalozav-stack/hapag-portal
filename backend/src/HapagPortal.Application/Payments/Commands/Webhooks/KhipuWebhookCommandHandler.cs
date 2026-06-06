namespace HapagPortal.Application.Payments.Commands.Webhooks;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class KhipuWebhookCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<KhipuWebhookCommand>
{
    private const string KhipuStatusDone = "done";
    private const string KhipuStatusRejected = "rejected";
    private const string KhipuStatusPending = "pending";

    public async Task<Result> Handle(
        KhipuWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.ExternalReference == request.ExternalReference, cancellationToken);

        if (payment is null)
            return Result.Failure(
                new Error("Payment.NotFound", $"No payment found with external reference '{request.ExternalReference}'."));

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
