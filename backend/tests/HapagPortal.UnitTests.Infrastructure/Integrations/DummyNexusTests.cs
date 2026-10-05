namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Infrastructure.Integrations.Nexus;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class DummyExemptionReaderTests
{
    private static readonly DateOnly At = new(2026, 10, 20);

    private readonly DummyExemptionReader _reader = new(Substitute.For<ILogger<DummyExemptionReader>>());

    [Fact]
    public async Task GetExemptionsAsync_ExemptTaxId_ShouldReturnGateInAndEds()
    {
        var result = await _reader.GetExemptionsAsync("76000001-1", null, At);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(e => e.Concept).Should().BeEquivalentTo(["GATE_IN", "EDS"]);
        result.Value.Should().OnlyContain(e => e.Amount == null && e.ValidFrom <= At);
    }

    [Theory]
    [InlineData("76000002-2")]
    [InlineData("76000003-3")]
    [InlineData("11111111-1")]
    public async Task GetExemptionsAsync_OtherTaxId_ShouldReturnEmptyList(string taxId)
    {
        var result = await _reader.GetExemptionsAsync(taxId, null, At);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExemptionsAsync_BeforeValidity_ShouldReturnEmptyList()
    {
        var result = await _reader.GetExemptionsAsync("76000001-1", null, new DateOnly(2025, 12, 31));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}

public sealed class DummyCreditConditionReaderTests
{
    private readonly DummyCreditConditionReader _reader = new(Substitute.For<ILogger<DummyCreditConditionReader>>());

    [Fact]
    public async Task GetConditionsAsync_CreditTaxId_ShouldReturnThirtyDayCredit()
    {
        var result = await _reader.GetConditionsAsync("76000002-2", "MC000202");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsFreightForwarder.Should().BeFalse();
        result.Value.MatchCode.Should().Be("MC000202");
        result.Value.Credit.Should().NotBeNull();
        result.Value.Credit!.CreditDays.Should().Be(30);
        result.Value.Credit.Concepts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetConditionsAsync_FreightForwarderTaxId_ShouldReturnFfwwWithoutCredit()
    {
        var result = await _reader.GetConditionsAsync("76000003-3", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFreightForwarder.Should().BeTrue();
        result.Value.Credit.Should().BeNull();
    }

    [Fact]
    public async Task GetConditionsAsync_ExemptTaxId_ShouldReturnConditionsWithoutCredit()
    {
        var result = await _reader.GetConditionsAsync("76000001-1", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFreightForwarder.Should().BeFalse();
        result.Value.Credit.Should().BeNull();
    }

    [Fact]
    public async Task GetConditionsAsync_UnknownTaxId_ShouldReturnSuccessWithNull()
    {
        var result = await _reader.GetConditionsAsync("11111111-1", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}

public sealed class DummyExchangeRateProviderTests
{
    private static readonly DateOnly Date = new(2026, 10, 20);

    private readonly DummyExchangeRateProvider _provider = new(Substitute.For<ILogger<DummyExchangeRateProvider>>());

    [Theory]
    [InlineData("USD", "CLP", 950)]
    [InlineData("USD", "BOB", 6.91)]
    [InlineData("USD", "EUR", 0.92)]
    [InlineData("usd", "clp", 950)]
    [InlineData("CLP", "CLP", 1)]
    public async Task GetRateAsync_KnownCurrencies_ShouldReturnApprovedRate(string from, string to, double expected)
    {
        var result = await _provider.GetRateAsync(from, to, Date);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Rate.Should().Be((decimal)expected);
        result.Value.FromCurrency.Should().Be(from.ToUpperInvariant());
        result.Value.ToCurrency.Should().Be(to.ToUpperInvariant());
        result.Value.EffectiveDate.Should().Be(Date);
        result.Value.Approved.Should().BeTrue();
    }

    [Fact]
    public async Task GetRateAsync_CrossRate_ShouldBeDerivedFromUsdBase()
    {
        var result = await _provider.GetRateAsync("CLP", "BOB", Date);

        result.Value!.Rate.Should().Be(Math.Round(6.91m / 950m, 6));
    }

    [Fact]
    public async Task GetRateAsync_UnknownCurrency_ShouldReturnSuccessWithNull()
    {
        var result = await _provider.GetRateAsync("USD", "JPY", Date);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}

public sealed class DummyTariffProviderTests
{
    private readonly DummyTariffProvider _provider = new(Substitute.For<ILogger<DummyTariffProvider>>());

    [Fact]
    public async Task GetTariffsAsync_ChileWarehouseChange_ShouldReturnTariff()
    {
        var result = await _provider.GetTariffsAsync("CL", "WAREHOUSE_CHANGE", new DateOnly(2026, 10, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Amount.Should().Be(9940m);
        result.Value[0].Currency.Should().Be("CLP");
    }

    [Theory]
    [InlineData("BO", "WAREHOUSE_CHANGE", "2026-10-20")]
    [InlineData("CL", "UNKNOWN", "2026-10-20")]
    [InlineData("CL", "WAREHOUSE_CHANGE", "2026-09-30")]
    public async Task GetTariffsAsync_NoTariff_ShouldReturnEmptyList(string country, string concept, string at)
    {
        var result = await _provider.GetTariffsAsync(country, concept, DateOnly.Parse(at));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
