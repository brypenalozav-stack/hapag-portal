namespace HapagPortal.Application.Receipts.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CreateReceiptCommand(Guid PaymentId) : ICommand<ReceiptResponseDto>;
