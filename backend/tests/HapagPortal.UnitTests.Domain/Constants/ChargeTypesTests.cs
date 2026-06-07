using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class ChargeTypesTests
{
    [Fact]
    public void GateIn_ShouldHaveCorrectValue()
    {
        ChargeTypes.GateIn.Should().Be("GateIn");
    }

    [Fact]
    public void GateOut_ShouldHaveCorrectValue()
    {
        ChargeTypes.GateOut.Should().Be("GateOut");
    }

    [Fact]
    public void EDS_ShouldHaveCorrectValue()
    {
        ChargeTypes.EDS.Should().Be("EDS");
    }

    [Fact]
    public void XMYAdminCharge_ShouldHaveCorrectValue()
    {
        ChargeTypes.XMYAdminCharge.Should().Be("XMYAdminCharge");
    }

    [Fact]
    public void Valorization_ShouldHaveCorrectValue()
    {
        ChargeTypes.Valorization.Should().Be("Valorization");
    }

    [Fact]
    public void TransshipmentCertificate_ShouldHaveCorrectValue()
    {
        ChargeTypes.TransshipmentCertificate.Should().Be("TransshipmentCertificate");
    }

    [Fact]
    public void Opening_ShouldHaveCorrectValue()
    {
        ChargeTypes.Opening.Should().Be("Opening");
    }
}
