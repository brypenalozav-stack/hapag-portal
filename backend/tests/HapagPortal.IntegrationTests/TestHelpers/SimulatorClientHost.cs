namespace HapagPortal.IntegrationTests.TestHelpers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Integrations.DbNet;
using HapagPortal.Infrastructure.Integrations.Fis;
using HapagPortal.Infrastructure.Integrations.Nexus;
using HapagPortal.Infrastructure.Integrations.Payments;
using HapagPortal.Infrastructure.Integrations.Tracking;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Contenedor real de las pruebas: <c>AddIntegrations</c> con <c>Mode=Real</c> para un sistema, la API key
/// desde un <see cref="ISecretResolver"/> sustituto y el handler primario del cliente apuntando al
/// simulador en memoria. Así se prueba la tubería completa (logging NF-27 + resiliencia) sin red.
/// </summary>
public static class SimulatorClientHost
{
    public const string ApiKey = "test-api-key";

    private static readonly Dictionary<string, (string Prefix, string ClientName)> Systems = new()
    {
        ["Nexus"] = ("nexus", nameof(HttpNexusClient)),
        ["Fis"] = ("fis", nameof(HttpShipmentSource)),
        ["Khipu"] = ("khipu", nameof(HttpKhipuPaymentProvider)),
        ["DbNet"] = ("dbnet", nameof(HttpInvoiceProvider)),
        ["Tracking"] = ("tracking", nameof(HttpTrackingProvider)),
    };

    public static ServiceProvider Build(
        WebApplicationFactory<Program> simulator,
        string system,
        SimulatorTraffic traffic,
        IDictionary<string, string?>? settings = null,
        ILoggerProvider? loggerProvider = null)
    {
        var (prefix, clientName) = Systems[system];

        var values = new Dictionary<string, string?>
        {
            [$"Integrations:{system}:Mode"] = "Real",
            [$"Integrations:{system}:BaseUrl"] = $"http://localhost/{prefix}",
            [$"Integrations:{system}:RetryBaseDelayMs"] = "50",
        };
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            values[$"Integrations:{system}:{key}"] = value;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var secretResolver = Substitute.For<ISecretResolver>();
        secretResolver.ResolveAsync(default!, default, default).ReturnsForAnyArgs(ApiKey);

        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            if (loggerProvider is not null)
                logging.AddProvider(loggerProvider);
        });
        services.AddSingleton(secretResolver);
        services.AddIntegrations(configuration);

        services.AddHttpClient(clientName)
            .ConfigurePrimaryHttpMessageHandler(() => new ScenarioHandler(traffic, simulator.Server.CreateHandler()));

        return services.BuildServiceProvider();
    }
}
