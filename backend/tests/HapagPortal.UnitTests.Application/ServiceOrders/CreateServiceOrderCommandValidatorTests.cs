namespace HapagPortal.UnitTests.Application.ServiceOrders;

using FluentAssertions;
using HapagPortal.Application.ServiceOrders.Commands.Create;

public sealed class CreateServiceOrderCommandValidatorTests
{
    private readonly CreateServiceOrderCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new CreateServiceOrderCommand("WarehouseChange", "Description", Guid.NewGuid(), "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyOrderType_ShouldFailValidation()
    {
        var command = new CreateServiceOrderCommand("", null, Guid.NewGuid(), "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderType");
    }

    [Fact]
    public void EmptyBillOfLadingId_ShouldFailValidation()
    {
        var command = new CreateServiceOrderCommand("WarehouseChange", null, Guid.Empty, "CL");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BillOfLadingId");
    }

    [Fact]
    public void EmptyCountry_ShouldFailValidation()
    {
        var command = new CreateServiceOrderCommand("WarehouseChange", null, Guid.NewGuid(), "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public void InvalidCountry_ShouldFailValidation()
    {
        var command = new CreateServiceOrderCommand("WarehouseChange", null, Guid.NewGuid(), "US");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Theory]
    [InlineData("CL")]
    [InlineData("BO")]
    public void ValidCountries_ShouldPassValidation(string country)
    {
        var command = new CreateServiceOrderCommand("WarehouseChange", null, Guid.NewGuid(), country);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
