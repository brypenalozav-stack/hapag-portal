using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class WarehouseChangeTests
{
    [Fact]
    public void NewWarehouseChange_ShouldHaveRequiredProperties()
    {
        // Arrange
        var blId = Guid.NewGuid();

        // Act
        var change = new WarehouseChange
        {
            FromWarehouse = "WH-A",
            ToWarehouse = "WH-B",
            Currency = "USD",
            Status = "Pending",
            Country = "CL",
            BillOfLadingId = blId
        };

        // Assert
        change.FromWarehouse.Should().Be("WH-A");
        change.ToWarehouse.Should().Be("WH-B");
        change.Currency.Should().Be("USD");
        change.Status.Should().Be("Pending");
        change.Country.Should().Be("CL");
        change.BillOfLadingId.Should().Be(blId);
    }

    [Fact]
    public void NewWarehouseChange_ShouldHaveDefaultValues()
    {
        // Act
        var change = new WarehouseChange
        {
            FromWarehouse = "WH-A",
            ToWarehouse = "WH-B",
            Currency = "USD",
            Status = "Pending",
            Country = "CL"
        };

        // Assert
        change.Id.Should().NotBeEmpty();
        change.Amount.Should().Be(0);
    }

    [Fact]
    public void WarehouseChange_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(WarehouseChange).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void WarehouseChange_ShouldBeSealed()
    {
        typeof(WarehouseChange).IsSealed.Should().BeTrue();
    }
}
