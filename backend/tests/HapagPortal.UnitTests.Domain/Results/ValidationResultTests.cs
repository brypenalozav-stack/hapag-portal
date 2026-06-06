using FluentAssertions;
using HapagPortal.Domain.Results;

namespace HapagPortal.UnitTests.Domain.Results;

public sealed class ValidationResultTests
{
    [Fact]
    public void WithErrors_ShouldContainAllErrors()
    {
        // Arrange
        var errors = new[]
        {
            Error.Validation("Name is required."),
            Error.Validation("Email is required.")
        };

        // Act
        var result = ValidationResult.WithErrors(errors);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void WithErrors_ShouldBeFailure()
    {
        // Arrange
        var errors = new[] { Error.Validation("Name is required.") };

        // Act
        var result = ValidationResult.WithErrors(errors);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Errors_ShouldImplementIValidationResult()
    {
        // Arrange
        var errors = new[] { Error.Validation("Name is required.") };

        // Act
        var result = ValidationResult.WithErrors(errors);

        // Assert
        result.Should().BeAssignableTo<IValidationResult>();
        ((IValidationResult)result).Errors.Should().HaveCount(1);
    }

    [Fact]
    public void GenericWithErrors_ShouldContainAllErrors()
    {
        // Arrange
        var errors = new[]
        {
            Error.Validation("Name is required."),
            Error.Validation("Email is required.")
        };

        // Act
        var result = ValidationResult<string>.WithErrors(errors);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.IsFailure.Should().BeTrue();
        result.Should().BeAssignableTo<IValidationResult>();
    }
}
