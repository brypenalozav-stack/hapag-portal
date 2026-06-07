using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class ServiceOrderStatusTests
{
    [Fact]
    public void Requested_ShouldHaveCorrectValue()
    {
        ServiceOrderStatus.Requested.Should().Be("Requested");
    }

    [Fact]
    public void InProgress_ShouldHaveCorrectValue()
    {
        ServiceOrderStatus.InProgress.Should().Be("InProgress");
    }

    [Fact]
    public void Completed_ShouldHaveCorrectValue()
    {
        ServiceOrderStatus.Completed.Should().Be("Completed");
    }

    [Fact]
    public void Cancelled_ShouldHaveCorrectValue()
    {
        ServiceOrderStatus.Cancelled.Should().Be("Cancelled");
    }
}
