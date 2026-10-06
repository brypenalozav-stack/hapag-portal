using HapagPortal.Application.Assistant;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations;
using HapagPortal.Infrastructure.Integrations.Assistant;
using HapagPortal.Infrastructure.Integrations.DbNet;
using HapagPortal.Infrastructure.Integrations.Fis;
using HapagPortal.Infrastructure.Integrations.Nexus;
using HapagPortal.Infrastructure.Integrations.Payments;
using HapagPortal.Infrastructure.Integrations.Signature;
using HapagPortal.Infrastructure.Integrations.Storage;
using HapagPortal.Infrastructure.Integrations.Tatc;
using HapagPortal.Infrastructure.Integrations.Tracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Polly;

namespace HapagPortal.Infrastructure.DependencyInjection;

public static partial class DependencyInjectionExtensions
{
    private const int DefaultTimeoutSeconds = 10;
    private const int DefaultTotalTimeoutSeconds = 30;
    private const int DefaultRetryBaseDelayMs = 2000;
    private const int DefaultTatcCacheSeconds = 30;
    private const string DefaultOllamaModel = "phi3.5";
    private const int DefaultOllamaTimeoutSeconds = 20;

    /// <summary>
    /// Registra los adaptadores de integración según <c>Integrations:&lt;Sistema&gt;:Mode</c>
    /// (<c>Dummy</c> por defecto). Con <c>Mode=Real</c>, Nexus, Fis, Khipu, BancoChile, DbNet, Tracking y Tatc
    /// usan su cliente HTTP con logging (NF-27) y resiliencia (Tatc además con caché corta, M2-09); Santander, Bci, Signature y Storage no
    /// tienen cliente Real y, como un modo desconocido o un <c>BaseUrl</c> inválido, detienen el arranque
    /// con <see cref="InvalidOperationException"/>. Storage admite además <c>Mode=Local</c>: archivos en
    /// <c>Integrations:Storage:LocalPath</c> (por defecto bajo LocalApplicationData) que persisten entre reinicios.
    /// </summary>
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Se validan todos los sistemas antes de registrar nada, para fallar en el arranque.
        var localStorage = IsLocalStorage(configuration);
        var realSystems = IntegrationSystems.All
            .Where(system => !(localStorage && system == IntegrationSystems.Storage) && IsRealMode(configuration, system))
            .ToHashSet(StringComparer.Ordinal);

        services.AddMetrics();
        services.TryAddSingleton<IntegrationMetrics>();

        if (realSystems.Contains(IntegrationSystems.Nexus))
        {
            services.AddRealClient<HttpNexusClient>(configuration, IntegrationSystems.Nexus);
            services.AddTransient<IExemptionReader>(sp => sp.GetRequiredService<HttpNexusClient>());
            services.AddTransient<ICreditConditionReader>(sp => sp.GetRequiredService<HttpNexusClient>());
            services.AddTransient<IExchangeRateProvider>(sp => sp.GetRequiredService<HttpNexusClient>());
            services.AddTransient<ITariffProvider>(sp => sp.GetRequiredService<HttpNexusClient>());
        }
        else
        {
            services.AddSingleton<IExemptionReader, DummyExemptionReader>();
            services.AddSingleton<ICreditConditionReader, DummyCreditConditionReader>();
            services.AddSingleton<IExchangeRateProvider, DummyExchangeRateProvider>();
            services.AddSingleton<ITariffProvider, DummyTariffProvider>();
        }

        if (realSystems.Contains(IntegrationSystems.Fis))
        {
            services.AddRealClient<HttpShipmentSource>(configuration, IntegrationSystems.Fis);
            services.AddTransient<IShipmentSource>(sp => sp.GetRequiredService<HttpShipmentSource>());
        }
        else
        {
            services.AddSingleton<IShipmentSource, DummyShipmentSource>();
        }

        foreach (var provider in IntegrationSystems.PaymentProviders)
        {
            if (realSystems.Contains(provider))
                continue;

            services.AddKeyedSingleton<IPaymentProvider>(provider, (sp, key) =>
                new DummyPaymentProvider((string)key!, sp.GetRequiredService<ILogger<DummyPaymentProvider>>()));
        }

        // Proveedores de pago Real como servicios con clave, para que [FromKeyedServices] resuelva.
        if (realSystems.Contains(IntegrationSystems.Khipu))
        {
            services.AddRealClient<HttpKhipuPaymentProvider>(configuration, IntegrationSystems.Khipu);
            services.AddKeyedTransient<IPaymentProvider>(IntegrationSystems.Khipu, (sp, _) =>
                sp.GetRequiredService<HttpKhipuPaymentProvider>());
        }

        if (realSystems.Contains(IntegrationSystems.BancoChile))
        {
            services.AddRealClient<HttpBancoChilePaymentProvider>(configuration, IntegrationSystems.BancoChile);
            services.AddKeyedTransient<IPaymentProvider>(IntegrationSystems.BancoChile, (sp, _) =>
                sp.GetRequiredService<HttpBancoChilePaymentProvider>());
        }

        // M5-03: el medio de pago configurado elige el proveedor por su clave.
        services.AddScoped<IPaymentProviderResolver, PaymentProviderResolver>();

        if (realSystems.Contains(IntegrationSystems.DbNet))
        {
            services.AddRealClient<HttpInvoiceProvider>(configuration, IntegrationSystems.DbNet);
            services.AddTransient<IInvoiceProvider>(sp => sp.GetRequiredService<HttpInvoiceProvider>());
        }
        else
        {
            services.AddSingleton<IInvoiceProvider, DummyInvoiceProvider>();
        }

        services.AddSingleton<IDocumentSigner, DummyDocumentSigner>();

        if (localStorage)
        {
            var root = configuration[$"Integrations:{IntegrationSystems.Storage}:LocalPath"];
            services.AddSingleton<IFileStorage>(sp => new LocalFileStorage(
                string.IsNullOrWhiteSpace(root) ? LocalFileStorage.DefaultRoot() : root,
                sp.GetRequiredService<ILogger<LocalFileStorage>>()));
        }
        else
        {
            services.AddSingleton<IFileStorage, DummyFileStorage>();
        }

        if (realSystems.Contains(IntegrationSystems.Tracking))
        {
            services.AddRealClient<HttpTrackingProvider>(configuration, IntegrationSystems.Tracking);
            services.AddTransient<ITrackingProvider>(sp => sp.GetRequiredService<HttpTrackingProvider>());
        }
        else
        {
            services.AddSingleton<ITrackingProvider, DummyTrackingProvider>();
        }

        // M2-09: estado del TATC vigente en el origen; solo caché corta (Integrations:Tatc:CacheSeconds).
        if (realSystems.Contains(IntegrationSystems.Tatc))
        {
            var cacheSeconds = configuration.GetValue($"Integrations:{IntegrationSystems.Tatc}:CacheSeconds", DefaultTatcCacheSeconds);
            services.AddMemoryCache();
            services.AddRealClient<HttpTatcProvider>(configuration, IntegrationSystems.Tatc);
            services.AddTransient<ITatcProvider>(sp => new CachedTatcProvider(
                sp.GetRequiredService<HttpTatcProvider>(),
                sp.GetRequiredService<IMemoryCache>(),
                TimeSpan.FromSeconds(Math.Max(0, cacheSeconds))));
        }
        else
        {
            services.AddSingleton<ITatcProvider, DummyTatcProvider>();
        }

        return services;
    }

    /// <summary>
    /// Motor del asistente según <c>Assistant:Mode</c> (M10-01): <c>Rules</c> por defecto (sin dependencias
    /// externas) u <c>Ollama</c>, modelo abierto local por HTTP, que exige <c>Assistant:Ollama:BaseUrl</c> absoluta
    /// (<c>Model</c> y <c>TimeoutSeconds</c> opcionales). Un modo desconocido o una URL inválida detienen el arranque,
    /// igual que las integraciones. Registra también <see cref="AssistantSettings"/>.
    /// </summary>
    public static IServiceCollection AddAssistantEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(AssistantSettings.SectionName).Get<AssistantSettings>() ?? new AssistantSettings();
        var mode = string.IsNullOrWhiteSpace(settings.Mode) ? AssistantEngineModes.Rules : settings.Mode.Trim();

        if (string.Equals(mode, AssistantEngineModes.Rules, StringComparison.OrdinalIgnoreCase))
        {
            settings.Mode = AssistantEngineModes.Rules;
            services.AddSingleton<IAssistantEngine>(sp => sp.GetRequiredService<RulesAssistantEngine>());
        }
        else if (string.Equals(mode, AssistantEngineModes.Ollama, StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = configuration["Assistant:Ollama:BaseUrl"]?.Trim();
            if (string.IsNullOrEmpty(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException("Assistant:Ollama:BaseUrl debe ser una URL absoluta cuando Assistant:Mode=Ollama.");

            var model = configuration["Assistant:Ollama:Model"];
            var timeoutSeconds = configuration.GetValue("Assistant:Ollama:TimeoutSeconds", DefaultOllamaTimeoutSeconds);

            settings.Mode = AssistantEngineModes.Ollama;
            services.AddSingleton(new OllamaEngineOptions(string.IsNullOrWhiteSpace(model) ? DefaultOllamaModel : model.Trim()));
            services.AddHttpClient<OllamaAssistantEngine>(c =>
            {
                c.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute);
                c.Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds));
            });
            services.AddTransient<IAssistantEngine>(sp => sp.GetRequiredService<OllamaAssistantEngine>());
        }
        else
        {
            throw new InvalidOperationException(
                $"Assistant:Mode='{mode}' no es válido. Valores admitidos: {AssistantEngineModes.Rules}, {AssistantEngineModes.Ollama}.");
        }

        services.AddSingleton(settings);
        return services;
    }

    /// <summary>
    /// Cliente tipado (nombre = <c>typeof(TClient).Name</c>) con logging NF-27 por fuera y la tubería
    /// estándar de resiliencia: timeout total, reintentos exponenciales, circuit breaker y timeout por intento.
    /// </summary>
    private static void AddRealClient<TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        string system)
        where TClient : class
    {
        var baseAddress = ReadBaseAddress(configuration, system);
        var timeoutSeconds = configuration.GetValue($"Integrations:{system}:TimeoutSeconds", DefaultTimeoutSeconds);
        var totalTimeoutSeconds = configuration.GetValue($"Integrations:{system}:TotalTimeoutSeconds", DefaultTotalTimeoutSeconds);
        var retryBaseDelayMs = configuration.GetValue($"Integrations:{system}:RetryBaseDelayMs", DefaultRetryBaseDelayMs);

        services.AddHttpClient<TClient>(c => c.BaseAddress = baseAddress)
            .AddHttpMessageHandler(sp => new IntegrationLoggingHandler(
                system,
                sp.GetRequiredService<ILogger<IntegrationLoggingHandler>>(),
                sp.GetRequiredService<IntegrationMetrics>()))
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(totalTimeoutSeconds);
                o.Retry.MaxRetryAttempts = 3;
                o.Retry.BackoffType = DelayBackoffType.Exponential;
                o.Retry.Delay = TimeSpan.FromMilliseconds(retryBaseDelayMs);
                o.CircuitBreaker.MinimumThroughput = 5;
                o.CircuitBreaker.FailureRatio = 0.5;
                // El handler estándar exige SamplingDuration >= 2 × AttemptTimeout.
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, 2 * timeoutSeconds));
                o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });
    }

    /// <summary>
    /// <c>BaseUrl</c> absoluta y terminada en "/": los clientes usan rutas relativas sin "/" inicial,
    /// así <c>http://host/nexus</c> + <c>exemptions</c> conserva el prefijo <c>/nexus</c>.
    /// </summary>
    private static Uri ReadBaseAddress(IConfiguration configuration, string system)
    {
        var baseUrl = configuration[$"Integrations:{system}:BaseUrl"]?.Trim();

        if (string.IsNullOrEmpty(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Integrations:{system}:BaseUrl debe ser una URL absoluta cuando Integrations:{system}:Mode=Real.");

        return new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute);
    }

    private static bool IsLocalStorage(IConfiguration configuration) =>
        string.Equals(
            configuration[$"Integrations:{IntegrationSystems.Storage}:Mode"],
            IntegrationModes.Local,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsRealMode(IConfiguration configuration, string system)
    {
        var mode = configuration[$"Integrations:{system}:Mode"];

        if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, IntegrationModes.Dummy, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(mode, IntegrationModes.Real, StringComparison.OrdinalIgnoreCase))
        {
            if (!IntegrationSystems.WithRealAdapter.Contains(system))
                throw new InvalidOperationException($"Integrations:{system}:Mode=Real no tiene adaptador disponible");

            ReadBaseAddress(configuration, system);
            return true;
        }

        throw new InvalidOperationException(
            $"Integrations:{system}:Mode='{mode}' no es válido. Valores admitidos: {IntegrationModes.Dummy}, {IntegrationModes.Real}.");
    }
}
