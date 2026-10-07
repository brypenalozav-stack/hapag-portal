using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class ChargeConceptCodesTests
{
    [Fact]
    public void ExemptibleConcepts_ShouldMatchTheNexusContract()
    {
        ChargeConceptCodes.NexusExemptible.Should().Equal("GATE_IN", "EDS", "GATE_OUT");
    }

    [Fact]
    public void ServiceCodes_ShouldMatchTheNexusConceptCodes()
    {
        ChargeConceptCodes.WarehouseChange.Should().Be("WAREHOUSE_CHANGE");
        ChargeConceptCodes.Ipo.Should().Be("IPO");
        ChargeConceptCodes.Mhd.Should().Be("MHD");
        ChargeConceptCodes.AdvanceDemurrageBo.Should().Be("ADVANCE_DEMURRAGE_BO");
    }
}
