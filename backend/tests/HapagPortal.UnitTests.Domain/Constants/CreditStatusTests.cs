using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class CreditStatusTests
{
    [Fact]
    public void Active_ShouldHaveCorrectValue()
    {
        CreditStatus.Active.Should().Be("Active");
    }

    [Fact]
    public void Suspended_ShouldHaveCorrectValue()
    {
        CreditStatus.Suspended.Should().Be("Suspended");
    }

    [Fact]
    public void Expired_ShouldHaveCorrectValue()
    {
        CreditStatus.Expired.Should().Be("Expired");
    }
}
