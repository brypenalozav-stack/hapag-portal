namespace HapagPortal.Application.Payments.Commands.Confirm;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record ConfirmPaymentCommand(Guid PaymentId) : ICommand<PaymentResponseDto>;
