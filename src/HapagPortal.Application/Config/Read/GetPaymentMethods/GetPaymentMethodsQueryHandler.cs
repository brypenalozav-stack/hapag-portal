namespace HapagPortal.Application.Config.Read.GetPaymentMethods;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

public sealed class GetPaymentMethodsQueryHandler
    : IQueryHandler<GetPaymentMethodsQuery, List<PaymentMethodResponseDto>>
{
    public Task<Result<List<PaymentMethodResponseDto>>> Handle(
        GetPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var methods = request.Country switch
        {
            CountryCodes.Chile =>
            [
                new PaymentMethodResponseDto(PaymentMethods.CreditCard, "Credit Card", "Pay with credit card", true),
                new PaymentMethodResponseDto(PaymentMethods.DebitCard, "Debit Card", "Pay with debit card", true),
                new PaymentMethodResponseDto(PaymentMethods.BankTransfer, "Bank Transfer", "Pay via bank transfer", true),
                new PaymentMethodResponseDto(PaymentMethods.WebPay, "WebPay", "Pay via WebPay", true),
            ],
            CountryCodes.Bolivia =>
            [
                new PaymentMethodResponseDto(PaymentMethods.BankTransfer, "Bank Transfer", "Pay via bank transfer", true),
                new PaymentMethodResponseDto(PaymentMethods.Cash, "Cash", "Pay with cash", true),
                new PaymentMethodResponseDto(PaymentMethods.Check, "Check", "Pay with check", true),
            ],
            _ => new List<PaymentMethodResponseDto>()
        };

        return Task.FromResult(Result<List<PaymentMethodResponseDto>>.Success(methods));
    }
}
