namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Integrations.DbNet;
using HapagPortal.Infrastructure.Integrations.Fis;
using HapagPortal.Infrastructure.Integrations.Nexus;
using HapagPortal.Infrastructure.Integrations.Payments;
using HapagPortal.Infrastructure.Integrations.Signature;
using HapagPortal.Infrastructure.Integrations.Storage;
using HapagPortal.Infrastructure.Integrations.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

public sealed class IntegrationsRegistrationTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ISecretResolver>());
        services.AddIntegrations(configuration);

        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> RealMode(params string[] systems) =>
        systems.SelectMany(system => new[]
            {
                KeyValuePair.Create($"Integrations:{system}:Mode", (string?)"Real"),
                KeyValuePair.Create($"Integrations:{system}:BaseUrl", (string?)$"http://localhost/{system.ToLowerInvariant()}"),
            })
            .ToDictionary(kv => kv.Key, kv => kv.Value);

    private static Action Register(string system, string mode) => () =>
        new ServiceCollection().AddIntegrations(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [$"Integrations:{system}:Mode"] = mode })
                .Build());

    [Fact]
    public void AddIntegrations_WithoutConfiguration_ShouldRegisterDummyAdapters()
    {
        using var provider = BuildProvider([]);

        provider.GetRequiredService<IExemptionReader>().Should().BeOfType<DummyExemptionReader>();
        provider.GetRequiredService<ICreditConditionReader>().Should().BeOfType<DummyCreditConditionReader>();
        provider.GetRequiredService<IExchangeRateProvider>().Should().BeOfType<DummyExchangeRateProvider>();
        provider.GetRequiredService<ITariffProvider>().Should().BeOfType<DummyTariffProvider>();
        provider.GetRequiredService<IShipmentSource>().Should().BeOfType<DummyShipmentSource>();
        provider.GetRequiredService<IInvoiceProvider>().Should().BeOfType<DummyInvoiceProvider>();
        provider.GetRequiredService<IDocumentSigner>().Should().BeOfType<DummyDocumentSigner>();
        provider.GetRequiredService<IFileStorage>().Should().BeOfType<DummyFileStorage>();
        provider.GetRequiredService<ITrackingProvider>().Should().BeOfType<DummyTrackingProvider>();
    }

    [Fact]
    public void AddIntegrations_ExplicitDummyMode_ShouldRegisterDummyAdapters()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Integrations:Nexus:Mode"] = "Dummy",
            ["Integrations:Fis:Mode"] = "dummy",
        });

        provider.GetRequiredService<IExemptionReader>().Should().BeOfType<DummyExemptionReader>();
        provider.GetRequiredService<IShipmentSource>().Should().BeOfType<DummyShipmentSource>();
    }

    [Theory]
    [InlineData("Khipu")]
    [InlineData("BancoChile")]
    [InlineData("Santander")]
    [InlineData("Bci")]
    public void AddIntegrations_ShouldRegisterKeyedDummyPaymentProvider(string key)
    {
        using var provider = BuildProvider([]);

        var paymentProvider = provider.GetRequiredKeyedService<IPaymentProvider>(key);

        paymentProvider.Should().BeOfType<DummyPaymentProvider>();
        paymentProvider.ProviderCode.Should().Be(key);
        paymentProvider.VerifiesNotifications.Should().BeFalse();
    }

    [Fact]
    public void AddIntegrations_RealMode_ShouldRegisterRealClients()
    {
        using var provider = BuildProvider(RealMode("Nexus", "Fis", "DbNet", "Tracking"));

        provider.GetRequiredService<IExemptionReader>().Should().BeOfType<HttpNexusClient>();
        provider.GetRequiredService<ICreditConditionReader>().Should().BeOfType<HttpNexusClient>();
        provider.GetRequiredService<IExchangeRateProvider>().Should().BeOfType<HttpNexusClient>();
        provider.GetRequiredService<ITariffProvider>().Should().BeOfType<HttpNexusClient>();
        provider.GetRequiredService<IShipmentSource>().Should().BeOfType<HttpShipmentSource>();
        provider.GetRequiredService<IInvoiceProvider>().Should().BeOfType<HttpInvoiceProvider>();
        provider.GetRequiredService<ITrackingProvider>().Should().BeOfType<HttpTrackingProvider>();

        // Los sistemas sin Mode=Real siguen en Dummy.
        provider.GetRequiredService<IDocumentSigner>().Should().BeOfType<DummyDocumentSigner>();
        provider.GetRequiredService<IFileStorage>().Should().BeOfType<DummyFileStorage>();
    }

    [Theory]
    [InlineData("Khipu", typeof(HttpKhipuPaymentProvider))]
    [InlineData("BancoChile", typeof(HttpBancoChilePaymentProvider))]
    public void AddIntegrations_RealMode_ShouldRegisterKeyedRealPaymentProvider(string key, Type expectedType)
    {
        using var provider = BuildProvider(RealMode(key));

        var paymentProvider = provider.GetRequiredKeyedService<IPaymentProvider>(key);

        paymentProvider.Should().BeOfType(expectedType);
        paymentProvider.ProviderCode.Should().Be(key);
        paymentProvider.VerifiesNotifications.Should().BeTrue();
    }

    [Fact]
    public void AddIntegrations_RealKhipu_ShouldKeepOtherPaymentProvidersDummy()
    {
        using var provider = BuildProvider(RealMode("Khipu"));

        provider.GetRequiredKeyedService<IPaymentProvider>("BancoChile").Should().BeOfType<DummyPaymentProvider>();
        provider.GetRequiredKeyedService<IPaymentProvider>("Santander").Should().BeOfType<DummyPaymentProvider>();
        provider.GetRequiredKeyedService<IPaymentProvider>("Bci").Should().BeOfType<DummyPaymentProvider>();
    }

    [Theory]
    [InlineData("Santander")]
    [InlineData("Bci")]
    [InlineData("Signature")]
    [InlineData("Storage")]
    public void AddIntegrations_RealModeWithoutAdapter_ShouldThrowFailFast(string system)
    {
        Register(system, "Real").Should().Throw<InvalidOperationException>()
            .WithMessage($"Integrations:{system}:Mode=Real no tiene adaptador disponible");
    }

    [Theory]
    [InlineData("Nexus")]
    [InlineData("Fis")]
    [InlineData("Khipu")]
    [InlineData("BancoChile")]
    [InlineData("DbNet")]
    [InlineData("Tracking")]
    public void AddIntegrations_RealModeWithoutBaseUrl_ShouldThrow(string system)
    {
        Register(system, "Real").Should().Throw<InvalidOperationException>()
            .WithMessage($"Integrations:{system}:BaseUrl debe ser una URL absoluta*");
    }

    [Theory]
    [InlineData("http://localhost/nexus")]
    [InlineData("http://localhost/nexus/")]
    public void AddIntegrations_RealMode_ShouldNormalizeBaseUrlWithTrailingSlash(string baseUrl)
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Integrations:Nexus:Mode"] = "Real",
            ["Integrations:Nexus:BaseUrl"] = baseUrl,
        });

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(HttpNexusClient));

        client.BaseAddress.Should().Be(new Uri("http://localhost/nexus/"));
        new Uri(client.BaseAddress!, "exemptions").AbsolutePath.Should().Be("/nexus/exemptions");
    }

    [Theory]
    [InlineData("Nexus", "Mock")]
    [InlineData("Storage", "1")]
    public void AddIntegrations_InvalidMode_ShouldThrow(string system, string mode)
    {
        Register(system, mode).Should().Throw<InvalidOperationException>()
            .WithMessage($"Integrations:{system}:Mode='{mode}'*");
    }
}
