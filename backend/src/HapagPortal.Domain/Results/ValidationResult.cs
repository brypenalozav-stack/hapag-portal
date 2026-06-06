namespace HapagPortal.Domain.Results;

public sealed class ValidationResult : Result, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(false, Error.Validation("One or more validation errors occurred."))
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationResult WithErrors(Error[] errors) => new(errors);
}

public sealed class ValidationResult<T> : Result<T>, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(false, Error.Validation("One or more validation errors occurred."))
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationResult<T> WithErrors(Error[] errors) => new(errors);
}
