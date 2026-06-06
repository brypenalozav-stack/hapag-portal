namespace HapagPortal.Application.BillsOfLading.Read.GetByNumber;

using FluentValidation;

public sealed class GetBLByNumberQueryValidator : AbstractValidator<GetBLByNumberQuery>
{
    public GetBLByNumberQueryValidator()
    {
        RuleFor(x => x.BLNumber)
            .NotEmpty().WithMessage("Bill of Lading number is required.");
    }
}
