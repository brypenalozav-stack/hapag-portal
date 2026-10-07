namespace HapagPortal.IntegrationTests.Integrations;

using FluentAssertions;
using HapagPortal.Application;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Domain.Results;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Integrations.Payments;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

/// <summary>
/// Los handlers de notificación, retorno y conciliación resuelven las pasarelas por su clave: se comprueba que el
/// contenedor real (MediatR + <c>AddIntegrations</c>) los arma en modo Dummy y en modo Real.
/// </summary>
public sealed class KhipuWebhookResolutionTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        services.AddSingleton(Substitute.For<IApplicationDbContext>());
        services.AddSingleton(Substitute.For<IWebhookAuthenticator>());
        services.AddSingleton(Substitute.For<ISecretResolver>());
        services.AddSingleton(Substitute.For<ICurrentUserService>());
        services.AddSingleton(Substitute.For<IShipmentAccessEvaluator>());
        services.AddIntegrations(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void ResolveHandlers(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<PaymentNotificationCommand, Result<PaymentNotificationAck>>>()
            .Should().BeOfType<PaymentNotificationCommandHandler>();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<ReconcileOnlinePaymentsCommand, Result<int>>>()
            .Should().BeOfType<ReconcileOnlinePaymentsCommandHandler>();
    }

    [Fact]
    public void DummyMode_ShouldResolveHandlersWithDummyProviders()
    {
        using var provider = Build([]);

        ResolveHandlers(provider);
        provider.GetRequiredKeyedService<IPaymentProvider>("Khipu").Should().BeOfType<DummyPaymentProvider>();
    }

    [Fact]
    public void RealMode_ShouldResolveHandlersWithRealProviders()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["Integrations:Khipu:Mode"] = "Real",
            ["Integrations:Khipu:BaseUrl"] = "http://localhost/khipu",
            ["Integrations:Getnet:Mode"] = "Real",
            ["Integrations:Getnet:BaseUrl"] = "http://localhost/getnet",
            ["Integrations:BciPagos:Mode"] = "Real",
            ["Integrations:BciPagos:BaseUrl"] = "http://localhost/bci",
        });

        ResolveHandlers(provider);

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredKeyedService<IPaymentProvider>("Khipu")
            .Should().BeOfType<HttpKhipuPaymentProvider>()
            .Which.VerifiesNotifications.Should().BeTrue();
        scope.ServiceProvider.GetRequiredKeyedService<IPaymentProvider>("Santander").Should().BeOfType<GetnetPaymentProvider>();
        scope.ServiceProvider.GetRequiredKeyedService<IPaymentProvider>("Bci").Should().BeOfType<BciPagosPaymentProvider>();
    }
}
