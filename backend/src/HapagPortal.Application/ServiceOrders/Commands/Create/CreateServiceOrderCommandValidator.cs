namespace HapagPortal.Application.ServiceOrders.Commands.Create;

using FluentValidation;

public sealed class CreateServiceOrderCommandValidator : AbstractValidator<CreateServiceOrderCommand>
{
    private static readonly string[] ValidCountries = ["CL", "BO"];

    public CreateServiceOrderCommandValidator()
    {
        RuleFor(x => x.OrderType)
            .NotEmpty().WithMessage("Order type is required.");

        RuleFor(x => x.BillOfLadingId)
            .NotEmpty().WithMessage("Bill of Lading ID is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Must(c => ValidCountries.Contains(c))
            .WithMessage($"Country must be one of: {string.Join(", ", ValidCountries)}.");
    }
}
