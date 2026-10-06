namespace HapagPortal.Application.Payments.Commands.Confirm;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Confirmación interna (Finanzas, p. ej. depósito verificado). Pasa por el ciclo de vida del pago: queda
/// en el historial (NF-02), asigna el comprobante y encola la liberación de los ítems (NF-03). Un pago ya
/// cobrado se confirma también durante un bloqueo de pagos (M8-07 bloquea iniciar, no reconocer abonos).
/// </summary>
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

        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.PendingVerification))
            return Result<PaymentResponseDto>.Failure(DomainErrors.Payment.InvalidStatus);

        var confirmed = await PaymentLifecycle.ConfirmAsync(
            dbContext, payment, PaymentActor.From(currentUserService), null, DateTime.UtcNow, cancellationToken);
        if (confirmed.IsFailure)
            return Result<PaymentResponseDto>.Failure(confirmed.Error);

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
