namespace HapagPortal.IntegrationTests.Integrations;

using FluentAssertions;
using HapagPortal.Application;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Domain.Results;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Integrations.Payments;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

/// <summary>
/// El handler del webhook de Khipu recibe <c>[FromKeyedServices("Khipu")] IPaymentProvider</c>: se
/// comprueba que el contenedor real (MediatR + <c>AddIntegrations</c>) lo resuelve en ambos modos.
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
        services.AddIntegrations(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static object ResolveHandler(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IRequestHandler<KhipuWebhookCommand, Result>>();
    }

    [Fact]
    public void DummyMode_ShouldResolveHandlerWithDummyProvider()
    {
        using var provider = Build([]);

        ResolveHandler(provider).Should().BeOfType<KhipuWebhookCommandHandler>();
        provider.GetRequiredKeyedService<IPaymentProvider>("Khipu").Should().BeOfType<DummyPaymentProvider>();
    }

    [Fact]
    public void RealMode_ShouldResolveHandlerWithRealProvider()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["Integrations:Khipu:Mode"] = "Real",
            ["Integrations:Khipu:BaseUrl"] = "http://localhost/khipu",
        });

        ResolveHandler(provider).Should().BeOfType<KhipuWebhookCommandHandler>();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredKeyedService<IPaymentProvider>("Khipu")
            .Should().BeOfType<HttpKhipuPaymentProvider>()
            .Which.VerifiesNotifications.Should().BeTrue();
    }
}
