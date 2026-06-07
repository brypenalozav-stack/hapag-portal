using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class LocalChargeTests
{
    [Fact]
    public void NewLocalCharge_ShouldHaveRequiredProperties()
    {
        // Arrange
        var blId = Guid.NewGuid();

        // Act
        var charge = new LocalCharge
        {
            ChargeType = "GateIn",
            Currency = "USD",
            Status = "Pending",
            BillOfLadingId = blId
        };

        // Assert
        charge.ChargeType.Should().Be("GateIn");
        charge.Currency.Should().Be("USD");
        charge.Status.Should().Be("Pending");
        charge.BillOfLadingId.Should().Be(blId);
    }

    [Fact]
    public void NewLocalCharge_ShouldHaveDefaultTaxValues()
    {
        // Act
        var charge = new LocalCharge
        {
            ChargeType = "GateIn",
            Currency = "USD",
            Status = "Pending"
        };

        // Assert
        charge.IsTaxable.Should().BeFalse();
        charge.TaxRate.Should().Be(0);
        charge.TaxAmount.Should().Be(0);
        charge.TotalAmount.Should().Be(0);
        charge.Amount.Should().Be(0);
    }

    [Fact]
    public void NewLocalCharge_ShouldHaveNullableDescription()
    {
        // Act
        var charge = new LocalCharge
        {
            ChargeType = "GateOut",
            Currency = "CLP",
            Status = "Active"
        };

        // Assert
        charge.Description.Should().BeNull();
    }

    [Fact]
    public void LocalCharge_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(LocalCharge).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void LocalCharge_ShouldBeSealed()
    {
        typeof(LocalCharge).IsSealed.Should().BeTrue();
    }
}
