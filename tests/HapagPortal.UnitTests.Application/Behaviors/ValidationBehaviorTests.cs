namespace HapagPortal.UnitTests.Application.Behaviors;

using FluentAssertions;
using FluentValidation;
using HapagPortal.Application.Common.Behaviors;
using HapagPortal.Domain.Results;
using MediatR;
using NSubstitute;
using FluentValidationResult = FluentValidation.Results.ValidationResult;
using FluentValidationFailure = FluentValidation.Results.ValidationFailure;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task ValidRequest_ShouldCallNext()
    {
        var validators = new List<IValidator<TestCommand>>
        {
            CreatePassingValidator()
        };

        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var expectedResult = Result<string>.Success("ok");

        var result = await behavior.Handle(
            new TestCommand(),
            () => Task.FromResult(expectedResult),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task InvalidRequest_ShouldReturnValidationResult()
    {
        var validators = new List<IValidator<TestCommand>>
        {
            CreateFailingValidator()
        };

        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var nextCalled = false;

        var result = await behavior.Handle(
            new TestCommand(),
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result<string>.Success("ok"));
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Should().BeAssignableTo<IValidationResult>();
        nextCalled.Should().BeFalse();
    }

    private static IValidator<TestCommand> CreatePassingValidator()
    {
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidationResult());
        return validator;
    }

    private static IValidator<TestCommand> CreateFailingValidator()
    {
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidationResult(new[]
            {
                new FluentValidationFailure("Property", "Error message")
            }));
        return validator;
    }

    public sealed record TestCommand : IRequest<Result<string>>;
}
