namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.Register;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new RegisterCommand(
            Name: "Test Client",
            TaxId: "12.345.678-9",
            Country: "CL",
            Email: "test@example.com",
            Password: "Password1!",
            ClientType: "Client",
            Phone: "+56912345678",
            AgentCode: null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyName_ShouldFailValidation()
    {
        var command = new RegisterCommand(
            Name: "",
            TaxId: "12.345.678-9",
            Country: "CL",
            Email: "test@example.com",
            Password: "Password1!",
            ClientType: "Client",
            Phone: null,
            AgentCode: null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void InvalidCountry_ShouldFailValidation()
    {
        var command = new RegisterCommand(
            Name: "Test Client",
            TaxId: "123456789",
            Country: "US",
            Email: "test@example.com",
            Password: "Password1!",
            ClientType: "Client",
            Phone: null,
            AgentCode: null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public void ShortPassword_ShouldFailValidation()
    {
        var command = new RegisterCommand(
            Name: "Test Client",
            TaxId: "12.345.678-9",
            Country: "CL",
            Email: "test@example.com",
            Password: "Pass1!",
            ClientType: "Client",
            Phone: null,
            AgentCode: null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void InvalidClientType_ShouldFailValidation()
    {
        var command = new RegisterCommand(
            Name: "Test Client",
            TaxId: "12.345.678-9",
            Country: "CL",
            Email: "test@example.com",
            Password: "Password1!",
            ClientType: "InvalidType",
            Phone: null,
            AgentCode: null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientType");
    }
}
