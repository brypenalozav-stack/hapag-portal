namespace HapagPortal.Application.Clients.Update.UpdateMyClient;

using FluentValidation;

public sealed class UpdateMyClientCommandValidator : AbstractValidator<UpdateMyClientCommand>
{
    public UpdateMyClientCommandValidator()
    {
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters.")
            .When(x => x.Address is not null);

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(x => x.City is not null);
    }
}
