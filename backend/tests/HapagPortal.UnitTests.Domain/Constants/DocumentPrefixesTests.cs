using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class DocumentPrefixesTests
{
    [Fact]
    public void Payment_ShouldHaveCorrectPrefix()
    {
        DocumentPrefixes.Payment.Should().Be("PAY-");
    }

    [Fact]
    public void Receipt_ShouldHaveCorrectPrefix()
    {
        DocumentPrefixes.Receipt.Should().Be("RCP-");
    }

    [Fact]
    public void ServiceOrder_ShouldHaveCorrectPrefix()
    {
        DocumentPrefixes.ServiceOrder.Should().Be("ODS-");
    }
}
