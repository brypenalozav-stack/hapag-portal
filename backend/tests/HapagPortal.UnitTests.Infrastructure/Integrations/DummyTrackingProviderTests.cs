namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Infrastructure.Integrations.Tracking;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class DummyTrackingProviderTests
{
    private readonly DummyTrackingProvider _provider = new(Substitute.For<ILogger<DummyTrackingProvider>>());

    [Fact]
    public async Task GetEventsAsync_HapagReference_ShouldReturnOrderedEvents()
    {
        var result = await _provider.GetEventsAsync("HLCUSCL2609A1234");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().BeInAscendingOrder(e => e.OccurredAt);
        result.Value.Select(e => e.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetEventsAsync_OtherReference_ShouldReturnEmptyList()
    {
        var result = await _provider.GetEventsAsync("UNKNOWN-REF");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
