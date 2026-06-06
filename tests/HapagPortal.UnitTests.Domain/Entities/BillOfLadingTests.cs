using FluentAssertions;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class BillOfLadingTests
{
    [Fact]
    public void NewBillOfLading_ShouldHaveDefaultValues()
    {
        // Act
        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "FCL",
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL"
        };

        // Assert
        bl.Id.Should().NotBeEmpty();
        bl.FreightAmount.Should().Be(0);
        bl.Vessel.Should().BeNull();
        bl.Voyage.Should().BeNull();
        bl.PortOfLoading.Should().BeNull();
        bl.PortOfDischarge.Should().BeNull();
        bl.ETD.Should().BeNull();
        bl.ETA.Should().BeNull();
        bl.Consignee.Should().BeNull();
        bl.Shipper.Should().BeNull();
        bl.NotifyParty.Should().BeNull();
        bl.Containers.Should().BeEmpty();
        bl.LocalCharges.Should().BeEmpty();
        bl.DemurrageCharges.Should().BeEmpty();
        bl.Payments.Should().BeEmpty();
    }

    [Fact]
    public void BillOfLading_ShouldBeSealed()
    {
        typeof(BillOfLading).IsSealed.Should().BeTrue();
    }
}
