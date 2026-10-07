namespace HapagPortal.UnitTests.Application.PortalLinks;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.PortalLinks;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Enlaces externos del país: portal de devoluciones y tarifarios oficiales, resueltos desde el parámetro global y,
/// si no, desde la configuración del despliegue; solo URL https absolutas.
/// </summary>
public sealed class ExternalLinksTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private void Setting(string key, string? value, string scope = ConfigurationScopes.Global) =>
        _db.ConfigurationSettingList.Add(new ConfigurationSetting { Scope = scope, Key = key, Value = value, CreatedBy = "t" });

    private Task<HapagPortal.Domain.Results.Result<ExternalLinksDto>> Handle(PortalLinkSettings settings, string? country = null) =>
        new GetExternalLinksQueryHandler(_db, _currentUser, settings).Handle(new GetExternalLinksQuery(country), CancellationToken.None);

    [Fact]
    public async Task Setting_ShouldOverrideAppSettings()
    {
        Setting(GetExternalLinksQueryHandler.RefundsSettingKey("CL"), " https://refunds.portal.example/cl ");
        Setting(GetExternalLinksQueryHandler.TariffSettingKey(LocalTariffCodes.InlandChile), "https://tariffs.portal.example/inland");
        var settings = new PortalLinkSettings
        {
            Refunds = { ["CL"] = "https://refunds.app.example/cl" },
            LocalTariffs = { [LocalTariffCodes.InlandChile] = "https://tariffs.app.example/inland", [LocalTariffCodes.LocalCharges] = "https://tariffs.app.example/local" }
        };

        var links = (await Handle(settings, "cl")).Value;

        links.Country.Should().Be("CL");
        links.Refunds.Should().Be(new ExternalLinkDto(GetExternalLinksQueryHandler.RefundsCode, "https://refunds.portal.example/cl", true, "Setting"));
        links.LocalTariffs.Select(t => t.Code).Should().Equal(LocalTariffCodes.All);
        links.LocalTariffs[0].Should().Be(new ExternalLinkDto(LocalTariffCodes.InlandChile, "https://tariffs.portal.example/inland", true, "Setting"));
        links.LocalTariffs[1].Should().Be(new ExternalLinkDto(LocalTariffCodes.DemurrageDetention, null, false, "None"));
        links.LocalTariffs[2].Should().Be(new ExternalLinkDto(LocalTariffCodes.LocalCharges, "https://tariffs.app.example/local", true, "AppSettings"));
    }

    [Fact]
    public async Task NonHttpsUrls_ShouldBeIgnored()
    {
        Setting(GetExternalLinksQueryHandler.RefundsSettingKey("CL"), "http://refunds.portal.example/cl");
        Setting(GetExternalLinksQueryHandler.TariffSettingKey(LocalTariffCodes.InlandChile), "javascript:alert(1)");
        var settings = new PortalLinkSettings
        {
            Refunds = { ["CL"] = "https://refunds.app.example/cl" },
            LocalTariffs = { [LocalTariffCodes.InlandChile] = "/relative/path" }
        };

        var links = (await Handle(settings, "CL")).Value;

        links.Refunds.Url.Should().Be("https://refunds.app.example/cl");
        links.Refunds.Source.Should().Be("AppSettings");
        links.LocalTariffs[0].Configured.Should().BeFalse();
        links.LocalTariffs[0].Url.Should().BeNull();
    }

    [Fact]
    public async Task ClientScopedSettings_ShouldBeIgnored()
    {
        Setting(GetExternalLinksQueryHandler.RefundsSettingKey("CL"), "https://refunds.client.example/cl", ConfigurationScopes.Client);

        var links = (await Handle(new PortalLinkSettings(), "CL")).Value;

        links.Refunds.Configured.Should().BeFalse();
    }

    [Fact]
    public async Task UnconfiguredRefunds_ShouldReportNotConfigured()
    {
        Setting(GetExternalLinksQueryHandler.RefundsSettingKey("CL"), "https://refunds.portal.example/cl");
        _currentUser.Country.Returns(CountryCodes.Bolivia);

        var links = (await Handle(new PortalLinkSettings())).Value;

        links.Country.Should().Be(CountryCodes.Bolivia);
        links.Refunds.Should().Be(new ExternalLinkDto(GetExternalLinksQueryHandler.RefundsCode, null, false, "None"));
    }

    [Fact]
    public async Task InvalidCountry_ShouldFallBackToChile()
    {
        var settings = new PortalLinkSettings { Refunds = { ["CL"] = "https://refunds.app.example/cl", ["BO"] = "https://refunds.app.example/bo" } };

        var links = (await Handle(settings, "AR")).Value;

        links.Country.Should().Be(CountryCodes.Chile);
        links.Refunds.Url.Should().Be("https://refunds.app.example/cl");
    }
}
