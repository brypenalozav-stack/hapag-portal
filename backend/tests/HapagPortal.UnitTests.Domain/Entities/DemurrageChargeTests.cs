using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class DemurrageChargeTests
{
    [Fact]
    public void NewDemurrageCharge_ShouldHaveRequiredProperties()
    {
        // Arrange
        var blId = Guid.NewGuid();

        // Act
        var charge = new DemurrageCharge
        {
            ContainerNumber = "CONT-001",
            Currency = "USD",
            Status = "Pending",
            BillOfLadingId = blId
        };

        // Assert
        charge.ContainerNumber.Should().Be("CONT-001");
        charge.Currency.Should().Be("USD");
        charge.Status.Should().Be("Pending");
        charge.BillOfLadingId.Should().Be(blId);
    }

    [Fact]
    public void NewDemurrageCharge_ShouldHaveDefaultNumericValues()
    {
        // Act
        var charge = new DemurrageCharge
        {
            ContainerNumber = "CONT-001",
            Currency = "USD",
            Status = "Pending"
        };

        // Assert
        charge.FreeDays.Should().Be(0);
        charge.DemurrageDays.Should().Be(0);
        charge.DailyRate.Should().Be(0);
        charge.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void NewDemurrageCharge_ShouldHaveDefaultBooleanValues()
    {
        // Act
        var charge = new DemurrageCharge
        {
            ContainerNumber = "CONT-001",
            Currency = "USD",
            Status = "Pending"
        };

        // Assert
        charge.IsExempt.Should().BeFalse();
        charge.ExemptReason.Should().BeNull();
    }

    [Fact]
    public void DemurrageCharge_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(DemurrageCharge).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void DemurrageCharge_ShouldBeSealed()
    {
        typeof(DemurrageCharge).IsSealed.Should().BeTrue();
    }
}
