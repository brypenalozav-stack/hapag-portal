namespace HapagPortal.Application.Admin.CreditClients.Commands.Update;

using FluentValidation;

public sealed class UpdateCreditClientCommandValidator : AbstractValidator<UpdateCreditClientCommand>
{
    private static readonly string[] ValidStatuses = ["Active", "Suspended", "Revoked"];

    public UpdateCreditClientCommandValidator()
    {
        RuleFor(x => x.CreditLimit)
            .GreaterThan(0).WithMessage("Credit limit must be greater than zero.")
            .When(x => x.CreditLimit is not null);

        RuleFor(x => x.CreditStatus)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Credit status must be one of: {string.Join(", ", ValidStatuses)}.")
            .When(x => x.CreditStatus is not null);
    }
}
