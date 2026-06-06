namespace HapagPortal.Application.Config.Read.GetPaymentMethods;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetPaymentMethodsQuery(string Country) : IQuery<List<PaymentMethodResponseDto>>;
