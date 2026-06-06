namespace HapagPortal.Application.Receipts.Read.GetById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetReceiptByIdQuery(Guid PaymentId) : IQuery<ReceiptResponseDto>;
