namespace HapagPortal.Application.ServiceOrders.Read.GetPdf;

using HapagPortal.Application.Common.Messaging;

public sealed record GetServiceOrderPdfQuery(Guid Id) : IQuery<byte[]>;
