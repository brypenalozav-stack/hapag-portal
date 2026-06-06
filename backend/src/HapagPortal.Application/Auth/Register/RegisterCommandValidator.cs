namespace HapagPortal.Application.Auth.Register;

using FluentValidation;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private static readonly string[] ValidCountries = ["CL", "BO"];
    private static readonly string[] ValidClientTypes = ["Client", "CustomsAgent"];

    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.TaxId)
            .NotEmpty().WithMessage("Tax ID is required.")
            .MaximumLength(20).WithMessage("Tax ID must not exceed 20 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Must(c => ValidCountries.Contains(c))
            .WithMessage("Country must be 'CL' or 'BO'.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ClientType)
            .NotEmpty().WithMessage("Client type is required.")
            .Must(ct => ValidClientTypes.Contains(ct))
            .WithMessage("Client type must be 'Client' or 'CustomsAgent'.");

        RuleFor(x => x.TaxId)
            .Matches(@"^\d{1,2}\.\d{3}\.\d{3}-[\dkK]$")
            .When(x => x.Country == "CL")
            .WithMessage("Chilean RUT format is invalid (expected: XX.XXX.XXX-X).");

        RuleFor(x => x.TaxId)
            .Matches(@"^\d{5,}$")
            .When(x => x.Country == "BO")
            .WithMessage("Bolivian NIT format is invalid (expected: numeric, at least 5 digits).");
    }
}
