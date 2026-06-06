using FluentAssertions;
using HapagPortal.Domain.Results;

namespace HapagPortal.UnitTests.Domain.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_ShouldNotBeFailure()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_ShouldBeFailure()
    {
        // Act
        var result = Result.Failure(Error.NotFound("Client"));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failure_ShouldNotBeSuccess()
    {
        // Act
        var result = Result.Failure(Error.NotFound("Client"));

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_ShouldContainError()
    {
        // Arrange
        var error = Error.NotFound("Client");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Create_WithTrue_ShouldBeSuccess()
    {
        // Act
        var result = Result.Create(true, Error.NotFound("Client"));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithFalse_ShouldBeFailure()
    {
        // Arrange
        var error = Error.NotFound("Client");

        // Act
        var result = Result.Create(false, error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
