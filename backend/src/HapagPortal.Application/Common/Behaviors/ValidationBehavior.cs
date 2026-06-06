namespace HapagPortal.Application.Common.Behaviors;

using FluentValidation;
using HapagPortal.Domain.Results;
using MediatR;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new Error(f.PropertyName, f.ErrorMessage))
            .Distinct()
            .ToArray();

        if (errors.Length != 0)
            return CreateValidationResult<TResponse>(errors);

        return await next();
    }

    private static TResponse CreateValidationResult<TResult>(Error[] errors)
        where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
            return (TResponse)(object)(ValidationResult.WithErrors(errors) as TResult)!;

        var validationResultType = typeof(ValidationResult<>)
            .MakeGenericType(typeof(TResult).GetGenericArguments()[0]);

        var withErrorsMethod = validationResultType.GetMethod(nameof(ValidationResult.WithErrors))!;

        return (TResponse)(object)(TResult)withErrorsMethod.Invoke(null, [errors])!;
    }
}
