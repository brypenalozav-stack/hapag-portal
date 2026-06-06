namespace HapagPortal.Application.Admin.DemurrageExemptions.Commands.Create;

using FluentValidation;

public sealed class CreateDemurrageExemptionCommandValidator : AbstractValidator<CreateDemurrageExemptionCommand>
{
    private static readonly string[] ValidCountries = ["CL", "BO"];

    public CreateDemurrageExemptionCommandValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("Client name is required.");

        RuleFor(x => x.TaxId)
            .NotEmpty().WithMessage("Tax ID is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Must(c => ValidCountries.Contains(c))
            .WithMessage($"Country must be one of: {string.Join(", ", ValidCountries)}.");
    }
}
