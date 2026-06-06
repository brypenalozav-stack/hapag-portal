namespace HapagPortal.Application.Payments.Commands.Confirm;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class ConfirmPaymentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<ConfirmPaymentCommand, PaymentResponseDto>
{
    public async Task<Result<PaymentResponseDto>> Handle(
        ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .Include(p => p.BillOfLading)
            .Include(p => p.Client)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
            return Result<PaymentResponseDto>.Failure(
                DomainErrors.Payment.NotFound(request.PaymentId));

        if (payment.Status == PaymentStatus.Confirmed)
            return Result<PaymentResponseDto>.Failure(DomainErrors.Payment.AlreadyConfirmed);

        if (payment.Status == PaymentStatus.Cancelled)
            return Result<PaymentResponseDto>.Failure(DomainErrors.Payment.AlreadyCancelled);

        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            return Result<PaymentResponseDto>.Failure(DomainErrors.Payment.InvalidStatus);

        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedAt = DateTime.UtcNow;
        payment.ConfirmedBy = currentUserService.Email;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PaymentResponseDto>.Success(
            new PaymentResponseDto(
                payment.Id,
                payment.PaymentNumber,
                payment.PaymentType,
                payment.PaymentMethod,
                payment.Amount,
                payment.TaxAmount,
                payment.TotalAmount,
                payment.Currency,
                payment.Status,
                payment.BillOfLading?.BLNumber ?? "",
                payment.BillOfLadingId,
                payment.ClientId,
                payment.Client?.Name,
                payment.Country,
                payment.DepositProofUrl,
                payment.CreatedAt,
                payment.ConfirmedAt,
                payment.Details?.Select(d => new PaymentDetailDto(
                    d.Id,
                    d.ConceptType,
                    d.Description,
                    d.Amount,
                    d.Currency)).ToList()));
    }
}
