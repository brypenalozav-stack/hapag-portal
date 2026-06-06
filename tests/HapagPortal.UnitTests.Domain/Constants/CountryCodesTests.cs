using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class CountryCodesTests
{
    [Fact]
    public void Chile_ShouldBeCL()
    {
        CountryCodes.Chile.Should().Be("CL");
    }

    [Fact]
    public void Bolivia_ShouldBeBO()
    {
        CountryCodes.Bolivia.Should().Be("BO");
    }
}
