namespace HapagPortal.UnitTests.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using HapagPortal.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

public sealed class WebhookAuthenticatorTests
{
    private const string Body = "{\"transactionId\":\"TX-1\",\"externalReference\":\"EXT-1\",\"status\":\"approved\",\"amount\":1190}";
    private const string SigningKey = "clave-de-firma-de-prueba";

    private static WebhookAuthenticator Create(string? signingKey = SigningKey, bool enabled = true) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Webhooks:Enabled"] = enabled.ToString(),
                ["Payments:Webhooks:BancoChile:Secret"] = "good-secret",
                ["Payments:Webhooks:BancoChile:SigningKey"] = signingKey,
            })
            .Build());

    private static string Sign(string body, string key) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

    [Fact]
    public void IsValidSignature_WithCorrectSignature_ShouldBeTrue()
    {
        Create().IsValidSignature("BancoChile", Body, Sign(Body, SigningKey)).Should().BeTrue();
    }

    [Fact]
    public void IsValidSignature_WithUppercaseHex_ShouldBeTrue()
    {
        Create().IsValidSignature("BancoChile", Body, Sign(Body, SigningKey).ToUpperInvariant()).Should().BeTrue();
    }

    [Fact]
    public void IsValidSignature_WithTamperedBody_ShouldBeFalse()
    {
        var signature = Sign(Body, SigningKey);

        Create().IsValidSignature("BancoChile", Body.Replace("1190", "1"), signature).Should().BeFalse();
    }

    [Fact]
    public void IsValidSignature_WithOtherKey_ShouldBeFalse()
    {
        Create().IsValidSignature("BancoChile", Body, Sign(Body, "otra-clave")).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-hex")]
    public void IsValidSignature_WithEmptyOrMalformedSignature_ShouldBeFalse(string? signature)
    {
        Create().IsValidSignature("BancoChile", Body, signature).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidSignature_WithoutSigningKey_ShouldReject(string? signingKey)
    {
        // Sin clave configurada, ni siquiera la firma de una clave vacía se acepta (fail-closed).
        Create(signingKey).IsValidSignature("BancoChile", Body, Sign(Body, string.Empty)).Should().BeFalse();
    }

    [Fact]
    public void IsValidSignature_WithWebhooksDisabled_ShouldBeFalse()
    {
        Create(enabled: false).IsValidSignature("BancoChile", Body, Sign(Body, SigningKey)).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ExistingSecretCheck_ShouldStillWork()
    {
        var authenticator = Create();

        authenticator.IsValid("BancoChile", "good-secret").Should().BeTrue();
        authenticator.IsValid("BancoChile", "wrong").Should().BeFalse();
    }
}
