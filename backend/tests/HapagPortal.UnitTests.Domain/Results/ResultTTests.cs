using FluentAssertions;
using HapagPortal.Domain.Results;

namespace HapagPortal.UnitTests.Domain.Results;

public sealed class ResultTTests
{
    [Fact]
    public void Success_ShouldContainValue()
    {
        // Arrange
        var value = "test-value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        var value = "test-value";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_ShouldNotHaveValue()
    {
        // Arrange
        var result = Result<string>.Failure(Error.NotFound("Client"));

        // Act
        var act = () => result.Value;

        // Assert
        result.IsFailure.Should().BeTrue();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithValue_ShouldBeSuccess()
    {
        // Arrange
        var value = "test-value";

        // Act
        var result = Result<string>.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithNull_ShouldBeFailure()
    {
        // Act
        var result = Result<string>.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }
}
