namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new ForgotPasswordCommand("user@example.com");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyEmail_ShouldFailValidation()
    {
        var command = new ForgotPasswordCommand("");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFailValidation()
    {
        var command = new ForgotPasswordCommand("not-an-email");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
