namespace HapagPortal.Application.Payments.Create;

using FluentValidation;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    private static readonly string[] ValidPaymentTypes =
        ["FREIGHT", "LOCAL_CHARGES", "DEMURRAGE", "COMBINED",
         "Freight", "LocalCharges", "Demurrage", "Combined"];

    private static readonly string[] ValidPaymentMethods =
        ["BANK_TRANSFER", "CREDIT_CARD", "CREDIT_LINE", "QR_PAYMENT",
         "CreditCard", "DebitCard", "BankTransfer", "WebPay", "Cash", "Check", "Khipu", "Deposit"];

    private static readonly string[] ValidCountries = ["CL", "BO"];

    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.BlId)
            .NotEmpty().WithMessage("Bill of Lading ID is required.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Payment type is required.")
            .Must(pt => ValidPaymentTypes.Contains(pt))
            .WithMessage($"Payment type must be one of: {string.Join(", ", ValidPaymentTypes)}.");

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Payment method is required.")
            .Must(pm => ValidPaymentMethods.Contains(pm))
            .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Must(c => ValidCountries.Contains(c))
            .WithMessage($"Country must be one of: {string.Join(", ", ValidCountries)}.");

        When(x => x.Details is not null && x.Details.Count > 0, () =>
        {
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.ConceptType)
                    .NotEmpty().WithMessage("Concept type is required.");

                detail.RuleFor(d => d.Description)
                    .NotEmpty().WithMessage("Description is required.");

                detail.RuleFor(d => d.Amount)
                    .GreaterThan(0).WithMessage("Amount must be greater than zero.");

                detail.RuleFor(d => d.Currency)
                    .NotEmpty().WithMessage("Currency is required.");
            });
        });
    }
}
