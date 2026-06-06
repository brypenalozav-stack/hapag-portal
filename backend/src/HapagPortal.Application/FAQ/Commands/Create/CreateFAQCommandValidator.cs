namespace HapagPortal.Application.FAQ.Commands.Create;

using FluentValidation;

public sealed class CreateFAQCommandValidator : AbstractValidator<CreateFAQCommand>
{
    private static readonly string[] ValidCountries = ["CL", "BO"];

    public CreateFAQCommandValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required.");

        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("Answer is required.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Must(c => ValidCountries.Contains(c))
            .WithMessage($"Country must be one of: {string.Join(", ", ValidCountries)}.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be zero or greater.");
    }
}
