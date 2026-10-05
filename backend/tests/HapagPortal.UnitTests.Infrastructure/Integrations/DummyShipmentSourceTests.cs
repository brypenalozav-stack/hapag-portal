namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Infrastructure.Integrations.Fis;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class DummyShipmentSourceTests
{
    private readonly DummyShipmentSource _source = new(Substitute.For<ILogger<DummyShipmentSource>>());

    [Fact]
    public async Task GetByBlNumberAsync_KnownBl_ShouldReturnShipment()
    {
        var result = await _source.GetByBlNumberAsync("HLCUSCL2609A1234");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ShipmentType.Should().Be("IMPORT");
        result.Value.TaxId.Should().Be("76000001-1");
    }

    [Fact]
    public async Task GetByBlNumberAsync_UnknownBl_ShouldReturnSuccessWithNull()
    {
        var result = await _source.GetByBlNumberAsync("HLCUXXX0000X0000");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetUpdatedSinceAsync_AllTypes_ShouldReturnAllShipments()
    {
        var result = await _source.GetUpdatedSinceAsync(DateTime.MinValue, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUpdatedSinceAsync_ShouldFilterByDateAndType()
    {
        var since = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);

        var imports = await _source.GetUpdatedSinceAsync(since, "IMPORT");
        var exports = await _source.GetUpdatedSinceAsync(since, "EXPORT");

        imports.Value.Should().ContainSingle(s => s.BlNumber == "HLCUSCL2609A5678");
        exports.Value.Should().ContainSingle(s => s.ShipmentType == "EXPORT");
    }

    [Fact]
    public async Task GetUpdatedSinceAsync_FutureDate_ShouldReturnEmptyList()
    {
        var result = await _source.GetUpdatedSinceAsync(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
