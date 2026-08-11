namespace HapagPortal.UnitTests.Infrastructure.Secrets;

using System.Security.Cryptography;
using FluentAssertions;
using HapagPortal.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;

public sealed class AesSecretProtectorTests
{
    private static AesSecretProtector CreateProtector()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:MasterKey"] = key })
            .Build();
        return new AesSecretProtector(config);
    }

    [Fact]
    public void ProtectThenUnprotect_ShouldRoundTrip()
    {
        var protector = CreateProtector();
        const string secret = "clave-aduana-super-secreta-123";

        var protectedValue = protector.Protect(secret);

        protectedValue.Should().NotBe(secret);            // cifrado, no en claro
        protector.Unprotect(protectedValue).Should().Be(secret);
    }

    [Fact]
    public void Protect_SameInputTwice_ProducesDifferentCiphertext()
    {
        var protector = CreateProtector();

        // Nonce aleatorio → dos cifrados del mismo texto difieren.
        protector.Protect("x").Should().NotBe(protector.Protect("x"));
    }

    [Fact]
    public void Protect_WithoutMasterKey_Throws()
    {
        var config = new ConfigurationBuilder().Build();
        var protector = new AesSecretProtector(config);

        var act = () => protector.Protect("x");

        act.Should().Throw<InvalidOperationException>();
    }
}
