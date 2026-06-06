namespace HapagPortal.Application.Payments.Read.GetById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetPaymentByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentByIdQuery, PaymentResponseDto>
{
    public async Task<Result<PaymentResponseDto>> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .Include(p => p.BillOfLading)
            .Include(p => p.Client)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (payment is null)
            return Result<PaymentResponseDto>.Failure(DomainErrors.Payment.NotFound(request.Id));

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
