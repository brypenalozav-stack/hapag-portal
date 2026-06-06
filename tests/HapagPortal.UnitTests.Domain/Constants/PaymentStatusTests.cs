using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class PaymentStatusTests
{
    [Fact]
    public void AllStatuses_ShouldBeDefined()
    {
        PaymentStatus.Pending.Should().Be("Pending");
        PaymentStatus.Confirmed.Should().Be("Confirmed");
        PaymentStatus.Failed.Should().Be("Failed");
        PaymentStatus.Cancelled.Should().Be("Cancelled");
        PaymentStatus.PendingVerification.Should().Be("PendingVerification");
    }

    [Fact]
    public void AllStatuses_ShouldNotBeNullOrEmpty()
    {
        PaymentStatus.Pending.Should().NotBeNullOrEmpty();
        PaymentStatus.Confirmed.Should().NotBeNullOrEmpty();
        PaymentStatus.Failed.Should().NotBeNullOrEmpty();
        PaymentStatus.Cancelled.Should().NotBeNullOrEmpty();
        PaymentStatus.PendingVerification.Should().NotBeNullOrEmpty();
    }
}
