namespace HapagPortal.Application.Users.Create;

using FluentValidation;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.RoleCode).NotEmpty();
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
    }
}
