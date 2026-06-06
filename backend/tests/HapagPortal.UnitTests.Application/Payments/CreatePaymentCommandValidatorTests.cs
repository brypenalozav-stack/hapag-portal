namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Payments.Create;

public sealed class CreatePaymentCommandValidatorTests
{
    private readonly CreatePaymentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new CreatePaymentCommand(
            BlId: Guid.NewGuid(),
            Type: "Freight",
            Method: "CreditCard",
            Details: new List<PaymentDetailRequest>
            {
                new("Freight", "Ocean freight charge", 1500m, "USD")
            },
            ChargeIds: null,
            Country: "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyBlId_ShouldFailValidation()
    {
        var command = new CreatePaymentCommand(
            BlId: Guid.Empty,
            Type: "Freight",
            Method: "CreditCard",
            Details: new List<PaymentDetailRequest>
            {
                new("Freight", "Ocean freight charge", 1500m, "USD")
            },
            ChargeIds: null,
            Country: "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BlId");
    }

    [Fact]
    public void EmptyPaymentType_ShouldFailValidation()
    {
        var command = new CreatePaymentCommand(
            BlId: Guid.NewGuid(),
            Type: "",
            Method: "CreditCard",
            Details: new List<PaymentDetailRequest>
            {
                new("Freight", "Ocean freight charge", 1500m, "USD")
            },
            ChargeIds: null,
            Country: "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void NullDetails_ShouldPassValidation()
    {
        // Details are now optional - the handler auto-generates them from BL data
        var command = new CreatePaymentCommand(
            BlId: Guid.NewGuid(),
            Type: "FREIGHT",
            Method: "BANK_TRANSFER",
            Details: null,
            ChargeIds: null,
            Country: "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyPaymentMethod_ShouldFailValidation()
    {
        var command = new CreatePaymentCommand(
            BlId: Guid.NewGuid(),
            Type: "Freight",
            Method: "",
            Details: new List<PaymentDetailRequest>
            {
                new("Freight", "Ocean freight charge", 1500m, "USD")
            },
            ChargeIds: null,
            Country: "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Method");
    }

    [Fact]
    public void FrontendFormatValues_ShouldPassValidation()
    {
        // Frontend sends SCREAMING_SNAKE_CASE values
        var command = new CreatePaymentCommand(
            BlId: Guid.NewGuid(),
            Type: "FREIGHT",
            Method: "BANK_TRANSFER",
            Details: null,
            ChargeIds: null,
            Country: "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
