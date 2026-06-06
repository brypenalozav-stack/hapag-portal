namespace HapagPortal.Domain.Results;

public class Result<T> : Result
{
    private readonly T? _value;

    protected Result(bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = default;
    }

    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static Result<T> Create(T? value) =>
        value is not null ? new(value, true, Error.None) : new(default, false, Error.NullValue);

    public static implicit operator Result<T>(T? value) => Create(value);

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public new static Result<T> Failure(Error error) => new(default, false, error);
}
