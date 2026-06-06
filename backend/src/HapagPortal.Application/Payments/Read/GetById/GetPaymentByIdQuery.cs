namespace HapagPortal.Application.Payments.Read.GetById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetPaymentByIdQuery(Guid Id) : IQuery<PaymentResponseDto>;
