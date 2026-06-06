namespace HapagPortal.Application.WarehouseChanges.Commands.Create;

using FluentValidation;

public sealed class CreateWarehouseChangeCommandValidator : AbstractValidator<CreateWarehouseChangeCommand>
{
    private static readonly string[] ValidCountries = ["CL", "BO"];

    public CreateWarehouseChangeCommandValidator()
    {
        RuleFor(x => x.FromWarehouse)
            .NotEmpty().WithMessage("From warehouse is required.");

        RuleFor(x => x.ToWarehouse)
            .NotEmpty().WithMessage("To warehouse is required.");

        RuleFor(x => x.BillOfLadingId)
            .NotEmpty().WithMessage("Bill of Lading ID is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Must(c => ValidCountries.Contains(c))
            .WithMessage($"Country must be one of: {string.Join(", ", ValidCountries)}.");
    }
}
