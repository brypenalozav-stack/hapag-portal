namespace HapagPortal.Application.Admin.CreditClients.Commands.Create;

using FluentValidation;

public sealed class CreateCreditClientCommandValidator : AbstractValidator<CreateCreditClientCommand>
{
    public CreateCreditClientCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Equal("BO").WithMessage("Country must be 'BO'.");

        RuleFor(x => x.CreditLimit)
            .GreaterThan(0).WithMessage("Credit limit must be greater than zero.");
    }
}
