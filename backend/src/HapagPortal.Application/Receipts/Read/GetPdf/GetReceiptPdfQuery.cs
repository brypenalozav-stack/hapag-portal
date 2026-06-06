namespace HapagPortal.Application.Receipts.Read.GetPdf;

using HapagPortal.Application.Common.Messaging;

public sealed record GetReceiptPdfQuery(Guid PaymentId) : IQuery<byte[]>;
