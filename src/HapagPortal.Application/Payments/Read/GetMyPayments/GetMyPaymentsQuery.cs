namespace HapagPortal.Application.Payments.Read.GetMyPayments;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetMyPaymentsQuery() : IQuery<List<PaymentResponseDto>>;
