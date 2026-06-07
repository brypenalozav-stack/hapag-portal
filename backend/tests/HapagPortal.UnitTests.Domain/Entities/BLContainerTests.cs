using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class BLContainerTests
{
    [Fact]
    public void NewBLContainer_ShouldHaveRequiredProperties()
    {
        // Arrange
        var blId = Guid.NewGuid();

        // Act
        var container = new BLContainer
        {
            ContainerNumber = "MSKU1234567",
            ContainerType = "40HC",
            Status = "Active",
            BillOfLadingId = blId
        };

        // Assert
        container.ContainerNumber.Should().Be("MSKU1234567");
        container.ContainerType.Should().Be("40HC");
        container.Status.Should().Be("Active");
        container.BillOfLadingId.Should().Be(blId);
    }

    [Fact]
    public void NewBLContainer_ShouldHaveNullableWeightAndSealNumber()
    {
        // Act
        var container = new BLContainer
        {
            ContainerNumber = "MSKU1234567",
            ContainerType = "20GP",
            Status = "Active"
        };

        // Assert
        container.Weight.Should().BeNull();
        container.SealNumber.Should().BeNull();
    }

    [Fact]
    public void BLContainer_ShouldSetWeightAndSealNumber()
    {
        // Act
        var container = new BLContainer
        {
            ContainerNumber = "MSKU1234567",
            ContainerType = "40HC",
            Status = "Active",
            Weight = 25000.50m,
            SealNumber = "SEAL-001"
        };

        // Assert
        container.Weight.Should().Be(25000.50m);
        container.SealNumber.Should().Be("SEAL-001");
    }

    [Fact]
    public void BLContainer_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(BLContainer).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void BLContainer_ShouldBeSealed()
    {
        typeof(BLContainer).IsSealed.Should().BeTrue();
    }
}
