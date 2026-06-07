using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class ServiceOrderTests
{
    [Fact]
    public void NewServiceOrder_ShouldHaveRequiredProperties()
    {
        // Arrange
        var blId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        // Act
        var order = new ServiceOrder
        {
            OrderNumber = "ODS-001",
            OrderType = "Inspection",
            Status = "Requested",
            Country = "CL",
            BillOfLadingId = blId,
            ClientId = clientId
        };

        // Assert
        order.OrderNumber.Should().Be("ODS-001");
        order.OrderType.Should().Be("Inspection");
        order.Status.Should().Be("Requested");
        order.Country.Should().Be("CL");
        order.BillOfLadingId.Should().Be(blId);
        order.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void NewServiceOrder_ShouldHaveDefaultValues()
    {
        // Act
        var order = new ServiceOrder
        {
            OrderNumber = "ODS-001",
            OrderType = "Inspection",
            Status = "Requested",
            Country = "CL"
        };

        // Assert
        order.Id.Should().NotBeEmpty();
        order.Description.Should().BeNull();
        order.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void ServiceOrder_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(ServiceOrder).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void ServiceOrder_ShouldBeSealed()
    {
        typeof(ServiceOrder).IsSealed.Should().BeTrue();
    }
}
