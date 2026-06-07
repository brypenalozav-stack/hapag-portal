namespace HapagPortal.UnitTests.Application.Clients;

using FluentAssertions;
using HapagPortal.Application.Clients.Update.UpdateMyClient;

public sealed class UpdateMyClientCommandValidatorTests
{
    private readonly UpdateMyClientCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new UpdateMyClientCommand("+56912345678", "Address 123", "Santiago");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NullValues_ShouldPassValidation()
    {
        var command = new UpdateMyClientCommand(null, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PhoneExceedsMaxLength_ShouldFailValidation()
    {
        var command = new UpdateMyClientCommand(new string('1', 21), null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public void AddressExceedsMaxLength_ShouldFailValidation()
    {
        var command = new UpdateMyClientCommand(null, new string('A', 201), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address");
    }

    [Fact]
    public void CityExceedsMaxLength_ShouldFailValidation()
    {
        var command = new UpdateMyClientCommand(null, null, new string('C', 101));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City");
    }

    [Fact]
    public void PhoneAtMaxLength_ShouldPassValidation()
    {
        var command = new UpdateMyClientCommand(new string('1', 20), null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
