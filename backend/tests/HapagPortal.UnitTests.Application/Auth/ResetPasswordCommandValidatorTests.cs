namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.ResetPassword;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "reset-token", "NewPassword1!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyEmail_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("", "token", "NewPassword1!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("not-an-email", "token", "NewPassword1!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyToken_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "", "NewPassword1!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }

    [Fact]
    public void EmptyPassword_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "token", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void ShortPassword_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "token", "Ab1!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void PasswordWithoutUppercase_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "token", "password1!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void PasswordWithoutLowercase_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "token", "PASSWORD1!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void PasswordWithoutDigit_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "token", "Password!!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void PasswordWithoutSpecialChar_ShouldFailValidation()
    {
        var command = new ResetPasswordCommand("user@example.com", "token", "Password12");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}
