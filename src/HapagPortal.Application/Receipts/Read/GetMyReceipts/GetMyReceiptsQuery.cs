namespace HapagPortal.Application.Receipts.Read.GetMyReceipts;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetMyReceiptsQuery() : IQuery<List<ReceiptResponseDto>>;
