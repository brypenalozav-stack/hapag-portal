namespace HapagPortal.Application.Payments.Commands.Cancel;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Anulación por el cliente con las reglas de M5-02 (<see cref="ReceiptCancellationPolicy"/>): solo antes
/// de emitir la boleta de depósito o de enviar el pago a la plataforma. Queda registrada con usuario, rol,
/// fecha y la transición de estado (NF-02, NF-14); los ítems vuelven al carro.
/// </summary>
public sealed class CancelPaymentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CancelPaymentCommand, PaymentResponseDto>
{
    private const string ClientCancellationReason = "Cancelled by the client";

    public async Task<Result<PaymentResponseDto>> Handle(
        CancelPaymentCommand request,
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

        // Impedir cancelar pagos de otro cliente (BUG-7).
        if (payment.ClientId != currentUserService.ClientId)
            return Result<PaymentResponseDto>.Failure(
                DomainErrors.Payment.NotFound(request.PaymentId));

        if (payment.Status == PaymentStatus.Confirmed)
            return Result<PaymentResponseDto>.Failure(DomainErrors.Payment.AlreadyConfirmed);

        if (payment.Status == PaymentStatus.Cancelled)
            return Result<PaymentResponseDto>.Failure(DomainErrors.Payment.AlreadyCancelled);

        var denial = ReceiptCancellationPolicy.ClientDenialReason(payment.Status);
        if (denial is not null)
        {
            return Result<PaymentResponseDto>.Failure(denial switch
            {
                ReceiptCancellationPolicy.SlipIssued => DomainErrors.PaymentFlow.SlipAlreadyIssued,
                ReceiptCancellationPolicy.InProgress => DomainErrors.PaymentFlow.InProgress,
                _ => DomainErrors.Payment.InvalidStatus
            });
        }

        var cancelled = await PaymentLifecycle.CancelAsync(
            dbContext,
            payment,
            PaymentActor.From(currentUserService),
            PaymentCancellationRoles.Client,
            ClientCancellationReason,
            DateTime.UtcNow,
            cancellationToken);
        if (cancelled.IsFailure)
            return Result<PaymentResponseDto>.Failure(cancelled.Error);

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
