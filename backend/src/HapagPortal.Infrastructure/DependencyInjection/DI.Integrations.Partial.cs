using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.Integrations;
using HapagPortal.Infrastructure.Integrations.DbNet;
using HapagPortal.Infrastructure.Integrations.Fis;
using HapagPortal.Infrastructure.Integrations.Nexus;
using HapagPortal.Infrastructure.Integrations.Payments;
using HapagPortal.Infrastructure.Integrations.Signature;
using HapagPortal.Infrastructure.Integrations.Storage;
using HapagPortal.Infrastructure.Integrations.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.DependencyInjection;

public static partial class DependencyInjectionExtensions
{
    /// <summary>
    /// Registra los adaptadores de integración según <c>Integrations:&lt;Sistema&gt;:Mode</c>
    /// (<c>Dummy</c> por defecto). En esta fase no hay adaptadores Real: <c>Mode=Real</c> o un modo
    /// desconocido detienen el arranque con <see cref="InvalidOperationException"/>.
    /// </summary>
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Se validan todos los sistemas antes de registrar nada, para fallar en el arranque.
        foreach (var system in IntegrationSystems.All)
            EnsureDummyMode(configuration, system);

        services.AddSingleton<IExemptionReader, DummyExemptionReader>();
        services.AddSingleton<ICreditConditionReader, DummyCreditConditionReader>();
        services.AddSingleton<IExchangeRateProvider, DummyExchangeRateProvider>();
        services.AddSingleton<ITariffProvider, DummyTariffProvider>();

        services.AddSingleton<IShipmentSource, DummyShipmentSource>();

        foreach (var provider in IntegrationSystems.PaymentProviders)
        {
            services.AddKeyedSingleton<IPaymentProvider>(provider, (sp, key) =>
                new DummyPaymentProvider((string)key!, sp.GetRequiredService<ILogger<DummyPaymentProvider>>()));
        }

        services.AddSingleton<IInvoiceProvider, DummyInvoiceProvider>();
        services.AddSingleton<IDocumentSigner, DummyDocumentSigner>();
        services.AddSingleton<IFileStorage, DummyFileStorage>();
        services.AddSingleton<ITrackingProvider, DummyTrackingProvider>();

        return services;
    }

    private static void EnsureDummyMode(IConfiguration configuration, string system)
    {
        var mode = configuration[$"Integrations:{system}:Mode"];

        if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, IntegrationModes.Dummy, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(mode, IntegrationModes.Real, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Integrations:{system}:Mode=Real no tiene adaptador disponible");

        throw new InvalidOperationException(
            $"Integrations:{system}:Mode='{mode}' no es válido. Valores admitidos: {IntegrationModes.Dummy}, {IntegrationModes.Real}.");
    }
}
