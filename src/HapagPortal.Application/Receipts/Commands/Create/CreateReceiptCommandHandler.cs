namespace HapagPortal.Application.Receipts.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class CreateReceiptCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateReceiptCommand, ReceiptResponseDto>
{
    public async Task<Result<ReceiptResponseDto>> Handle(
        CreateReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
            return Result<ReceiptResponseDto>.Failure(
                DomainErrors.Payment.NotFound(request.PaymentId));

        if (payment.Status != PaymentStatus.Confirmed)
            return Result<ReceiptResponseDto>.Failure(DomainErrors.Payment.InvalidStatus);

        if (string.IsNullOrEmpty(payment.ReceiptNumber))
        {
            payment.ReceiptNumber = $"{DocumentPrefixes.Receipt}{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<ReceiptResponseDto>.Success(
            new ReceiptResponseDto(
                payment.Id,
                payment.ReceiptNumber,
                payment.Id,
                payment.PaymentNumber,
                payment.Amount,
                payment.TaxAmount,
                payment.TotalAmount,
                payment.Currency,
                payment.Country,
                payment.ConfirmedAt ?? DateTime.UtcNow));
    }
}
