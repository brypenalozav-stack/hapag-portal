namespace HapagPortal.UnitTests.Application.WarehouseChanges;

using FluentAssertions;
using HapagPortal.Application.WarehouseChanges.Commands.Create;

public sealed class CreateWarehouseChangeCommandValidatorTests
{
    private readonly CreateWarehouseChangeCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", Guid.NewGuid(), "CL", 500m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyFromWarehouse_ShouldFailValidation()
    {
        var command = new CreateWarehouseChangeCommand("", "Warehouse B", Guid.NewGuid(), "CL", 500m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FromWarehouse");
    }

    [Fact]
    public void EmptyToWarehouse_ShouldFailValidation()
    {
        var command = new CreateWarehouseChangeCommand("Warehouse A", "", Guid.NewGuid(), "CL", 500m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToWarehouse");
    }

    [Fact]
    public void EmptyBillOfLadingId_ShouldFailValidation()
    {
        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", Guid.Empty, "CL", 500m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BillOfLadingId");
    }

    [Fact]
    public void EmptyCountry_ShouldFailValidation()
    {
        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", Guid.NewGuid(), "", 500m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public void InvalidCountry_ShouldFailValidation()
    {
        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", Guid.NewGuid(), "US", 500m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Theory]
    [InlineData("CL")]
    [InlineData("BO")]
    public void ValidCountries_ShouldPassValidation(string country)
    {
        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", Guid.NewGuid(), country, 500m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
