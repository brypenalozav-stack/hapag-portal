namespace HapagPortal.UnitTests.Infrastructure.Services;

using FluentAssertions;
using HapagPortal.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

/// <summary>Secreto compartido de las notificaciones simuladas (adaptadores Dummy) e interruptor de los webhooks.</summary>
public sealed class WebhookAuthenticatorTests
{
    private static WebhookAuthenticator Create(string? secret = "good-secret", bool enabled = true) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Webhooks:Enabled"] = enabled.ToString(),
                ["Payments:Webhooks:Khipu:Secret"] = secret,
            })
            .Build());

    [Fact]
    public void IsValid_WithConfiguredSecret_ShouldCompareExactly()
    {
        var authenticator = Create();

        authenticator.IsValid("Khipu", "good-secret").Should().BeTrue();
        authenticator.IsValid("Khipu", "wrong").Should().BeFalse();
        authenticator.IsValid("Khipu", null).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_WithoutConfiguredSecret_ShouldReject(string? secret)
    {
        Create(secret).IsValid("Khipu", "").Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithWebhooksDisabled_ShouldReject()
    {
        var authenticator = Create(enabled: false);

        authenticator.WebhooksEnabled.Should().BeFalse();
        authenticator.IsValid("Khipu", "good-secret").Should().BeFalse();
    }
}
