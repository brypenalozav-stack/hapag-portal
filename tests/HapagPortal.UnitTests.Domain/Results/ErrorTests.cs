using FluentAssertions;
using HapagPortal.Domain.Results;

namespace HapagPortal.UnitTests.Domain.Results;

public sealed class ErrorTests
{
    [Fact]
    public void Error_None_ShouldHaveEmptyCodeAndMessage()
    {
        // Act
        var error = Error.None;

        // Assert
        error.Code.Should().BeEmpty();
        error.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_NotFound_ShouldContainEntityInfo()
    {
        // Act
        var error = Error.NotFound("Client");

        // Assert
        error.Code.Should().Be("Client.NotFound");
        error.Message.Should().Contain("Client");
        error.Message.Should().Contain("was not found");
    }

    [Fact]
    public void Error_Exists_ShouldContainEntityInfo()
    {
        // Act
        var error = Error.Exists("Client");

        // Assert
        error.Code.Should().Be("Client.Exists");
        error.Message.Should().Contain("Client");
        error.Message.Should().Contain("already exists");
    }

    [Fact]
    public void Error_HasData_ShouldContainEntityInfo()
    {
        // Act
        var error = Error.HasData("Client");

        // Assert
        error.Code.Should().Be("Client.HasData");
        error.Message.Should().Contain("Client");
        error.Message.Should().Contain("has associated data");
    }

    [Fact]
    public void Error_Validation_ShouldContainMessage()
    {
        // Arrange
        var message = "Name is required.";

        // Act
        var error = Error.Validation(message);

        // Assert
        error.Code.Should().Be("Error.Validation");
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Error_Unauthorized_ShouldHaveCorrectCode()
    {
        // Act
        var error = Error.Unauthorized;

        // Assert
        error.Code.Should().Be("Error.Unauthorized");
        error.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void Error_Forbidden_ShouldHaveCorrectCode()
    {
        // Act
        var error = Error.Forbidden;

        // Assert
        error.Code.Should().Be("Error.Forbidden");
        error.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void Error_Equality_SameCodeAndMessage_ShouldBeEqual()
    {
        // Arrange
        var error1 = new Error("Test.Code", "Test message");
        var error2 = new Error("Test.Code", "Test message");

        // Assert
        error1.Should().Be(error2);
    }

    [Fact]
    public void Error_Equality_DifferentCode_ShouldNotBeEqual()
    {
        // Arrange
        var error1 = new Error("Test.Code1", "Test message");
        var error2 = new Error("Test.Code2", "Test message");

        // Assert
        error1.Should().NotBe(error2);
    }

    [Fact]
    public void Error_NullValue_ShouldHaveCorrectCode()
    {
        // Act
        var error = Error.NullValue;

        // Assert
        error.Code.Should().Be("Error.NullValue");
        error.Message.Should().Contain("null");
    }
}
