namespace HapagPortal.UnitTests.Application.Config;

using FluentAssertions;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>Cierre de Fase 1: flags de funcionalidades con Fase 2 apagada por defecto.</summary>
public sealed class FeatureSettingsTests
{
    [Fact]
    public void Defaults_ShouldTurnOffPhaseTwoExceptReleaseLetterAndCounter()
    {
        var features = new FeatureSettings();

        features.IsEnabled(FeatureNames.ReleaseLetter).Should().BeTrue();
        features.IsEnabled(FeatureNames.Counter).Should().BeTrue();
        features.Snapshot().Where(f => f.Value).Select(f => f.Key)
            .Should().BeEquivalentTo(FeatureNames.ReleaseLetter, FeatureNames.Counter);
        features.Snapshot().Keys.Should().BeEquivalentTo(FeatureNames.All);
    }

    [Fact]
    public void Overrides_ShouldApplyCaseInsensitivelyAndIgnoreUnknownFlags()
    {
        var features = new FeatureSettings(new Dictionary<string, bool>
        {
            ["onDemandServices"] = true,
            [FeatureNames.ReleaseLetter] = false,
            ["Unknown"] = true,
        });

        features.IsEnabled(FeatureNames.OnDemandServices).Should().BeTrue();
        features.IsEnabled(FeatureNames.ReleaseLetter).Should().BeFalse();
        features.IsEnabled("Unknown").Should().BeFalse();
        features.Snapshot().Should().NotContainKey("Unknown");
    }

    [Fact]
    public async Task GetFeatures_ShouldReturnEveryFlagWithItsState()
    {
        var features = new FeatureSettings(new Dictionary<string, bool> { [FeatureNames.Announcements] = true });

        var result = await new GetFeaturesQueryHandler(features).Handle(new GetFeaturesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(FeatureNames.All.Count);
        result.Value[FeatureNames.Announcements].Should().BeTrue();
        result.Value[FeatureNames.OnDemandServices].Should().BeFalse();
        result.Value[FeatureNames.ReleaseLetter].Should().BeTrue();
    }

    private static ServiceRequest Request(string number, string code) => new()
    {
        RequestNumber = number,
        DefinitionCode = code,
        BlNumber = "BL-1",
        Country = CountryCodes.Chile,
        Operation = ServiceOperations.Import,
        Status = ServiceRequestStatus.InProgress,
    };

    [Theory]
    [InlineData(false, true, new[] { "RL" })]
    [InlineData(true, true, new[] { "RL", "OD" })]
    [InlineData(true, false, new[] { "OD" })]
    [InlineData(false, false, new string[0])]
    public void ServiceRequestFilter_ShouldFollowOnDemandAndReleaseLetterFlags(bool onDemand, bool releaseLetter, string[] expected)
    {
        var requests = new List<ServiceRequest>
        {
            Request("RL", ServiceDefinitionCodes.ReleaseLetter),
            Request("OD", ServiceDefinitionCodes.BlCorrection),
            Request("IAO", ServiceDefinitionCodes.IaoReinvoicing),
        };
        var features = new FeatureSettings(new Dictionary<string, bool>
        {
            [FeatureNames.OnDemandServices] = onDemand,
            [FeatureNames.ReleaseLetter] = releaseLetter,
        });

        requests.AsQueryable().VisibleFor(features).Select(r => r.RequestNumber).Should().BeEquivalentTo(expected);
        ServiceRequestFeatureFilter.IsVisible(ServiceDefinitionCodes.ReleaseLetter, features).Should().Be(releaseLetter);
        ServiceRequestFeatureFilter.IsVisible(ServiceDefinitionCodes.BlCorrection, features).Should().Be(onDemand);
    }
}
