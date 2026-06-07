namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.ConfirmEmail;

public sealed class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new ConfirmEmailCommand("user@example.com", "confirm-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyEmail_ShouldFailValidation()
    {
        var command = new ConfirmEmailCommand("", "confirm-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFailValidation()
    {
        var command = new ConfirmEmailCommand("not-an-email", "confirm-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyToken_ShouldFailValidation()
    {
        var command = new ConfirmEmailCommand("user@example.com", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }
}
