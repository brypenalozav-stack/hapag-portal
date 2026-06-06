namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.Login;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new LoginCommand("user@example.com", "Password123");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyEmail_ShouldFailValidation()
    {
        var command = new LoginCommand("", "Password123");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFailValidation()
    {
        var command = new LoginCommand("not-an-email", "Password123");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyPassword_ShouldFailValidation()
    {
        var command = new LoginCommand("user@example.com", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void ShortPassword_ShouldFailValidation()
    {
        var command = new LoginCommand("user@example.com", "12345");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
