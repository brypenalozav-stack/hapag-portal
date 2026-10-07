using FluentAssertions;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Charges;

/// <summary>Aplicación de tarifas con tramos del mantenedor (M8-01).</summary>
public sealed class TariffCalculatorTests
{
    private static readonly TierDefinition[] HourTiers =
    [
        new(0, 24, 100m),
        new(25, 48, 200m),
        new(49, null, 350m),
    ];

    private static readonly TierDefinition[] DemurrageTiers =
    [
        new(1, 7, 0m),
        new(8, 14, 45000m),
        new(15, null, 65000m),
    ];

    [Theory]
    [InlineData(0, 100)]
    [InlineData(24, 100)]
    [InlineData(25, 200)]
    [InlineData(200, 350)]
    public void Flat_ShouldReturnTheValueOfTheTierContainingTheMeasure(int hours, decimal expected)
    {
        var result = TariffCalculator.Compute(0m, TariffTierModes.Flat, HourTiers, hours);

        result.Amount.Should().Be(expected);
        result.Covered.Should().BeTrue();
    }

    [Fact]
    public void PerUnit_ShouldChargeEachDayAtItsTier()
    {
        var result = TariffCalculator.Compute(0m, TariffTierModes.PerUnit, DemurrageTiers, 17);

        result.Amount.Should().Be(7 * 45000m + 3 * 65000m);
        result.Lines.Should().HaveCount(3);
        result.Lines[1].Should().Be(new TariffBreakdownLine(8, 14, 7, 45000m, 315000m));
    }

    [Fact]
    public void PerUnit_WithNegotiatedFreeDays_ShouldNotChargeThem()
    {
        var result = TariffCalculator.Compute(0m, TariffTierModes.PerUnit, DemurrageTiers, 17, freeUnits: 10);

        result.Amount.Should().Be(4 * 45000m + 3 * 65000m);
    }

    [Fact]
    public void WithoutTiers_ShouldReturnTheBaseAmount()
    {
        TariffCalculator.Compute(9940m, TariffTierModes.Flat, [], 1).Amount.Should().Be(9940m);
    }

    [Fact]
    public void Flat_OutsideTheTiers_ShouldNotBeCovered()
    {
        var result = TariffCalculator.Compute(0m, TariffTierModes.Flat, [new TierDefinition(1, 10, 5m)], 0);

        result.Covered.Should().BeFalse();
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void ValidateTiers_ShouldRejectOverlapsAndOpenEndedTiersBeforeTheLast()
    {
        TariffCalculator.ValidateTiers(HourTiers).Should().BeNull();
        TariffCalculator.ValidateTiers([new(0, 24, 1m), new(24, null, 2m)]).Should().NotBeNull();
        TariffCalculator.ValidateTiers([new(0, null, 1m), new(25, null, 2m)]).Should().NotBeNull();
        TariffCalculator.ValidateTiers([new(5, 2, 1m)]).Should().NotBeNull();
        TariffCalculator.ValidateTiers([new(0, 2, -1m)]).Should().NotBeNull();
    }

    [Theory]
    [InlineData("2026-09-30", false)]
    [InlineData("2026-10-01", true)]
    [InlineData("2026-12-31", true)]
    [InlineData("2027-01-01", false)]
    public void IsInForce_ShouldIncludeBothValidityBounds(string date, bool expected)
    {
        TariffCalculator.IsInForce(new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31), DateOnly.Parse(date))
            .Should().Be(expected);
    }
}
