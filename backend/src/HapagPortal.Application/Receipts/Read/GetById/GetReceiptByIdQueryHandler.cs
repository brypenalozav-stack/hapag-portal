namespace HapagPortal.Application.Receipts.Read.GetById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetReceiptByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetReceiptByIdQuery, ReceiptResponseDto>
{
    public async Task<Result<ReceiptResponseDto>> Handle(
        GetReceiptByIdQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.ReceiptNumber != null, cancellationToken);

        if (payment is null)
            return Result<ReceiptResponseDto>.Failure(
                DomainErrors.Payment.NotFound(request.PaymentId));

        return Result<ReceiptResponseDto>.Success(
            new ReceiptResponseDto(
                payment.Id,
                payment.ReceiptNumber!,
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
