namespace HapagPortal.Application.Payments.Commands.Webhooks;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class BancoChileWebhookCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<BancoChileWebhookCommand>
{
    private const string BancoChileStatusApproved = "approved";
    private const string BancoChileStatusRejected = "rejected";
    private const string BancoChileStatusPending = "pending";

    public async Task<Result> Handle(
        BancoChileWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.ExternalReference == request.ExternalReference, cancellationToken);

        if (payment is null)
            return Result.Failure(
                new Error("Payment.NotFound", $"No payment found with external reference '{request.ExternalReference}'."));

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
