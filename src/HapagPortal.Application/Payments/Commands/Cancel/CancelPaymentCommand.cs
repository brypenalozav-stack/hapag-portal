namespace HapagPortal.Application.Payments.Commands.Cancel;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CancelPaymentCommand(Guid PaymentId) : ICommand<PaymentResponseDto>;
