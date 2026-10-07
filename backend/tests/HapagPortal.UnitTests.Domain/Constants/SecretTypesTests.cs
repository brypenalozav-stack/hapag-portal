using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class SecretTypesTests
{
    [Fact]
    public void SiiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.SiiKey.Should().Be("SII_KEY");
    }

    [Fact]
    public void CustomsUser_ShouldHaveCorrectValue()
    {
        SecretTypes.CustomsUser.Should().Be("CUSTOMS_USER");
    }

    [Fact]
    public void CustomsPassword_ShouldHaveCorrectValue()
    {
        SecretTypes.CustomsPassword.Should().Be("CUSTOMS_PASSWORD");
    }

    [Fact]
    public void CustomsCertificate_ShouldHaveCorrectValue()
    {
        SecretTypes.CustomsCertificate.Should().Be("CUSTOMS_CERTIFICATE");
    }

    [Fact]
    public void NexusApiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.NexusApiKey.Should().Be("NEXUS_API_KEY");
    }

    [Fact]
    public void FisApiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.FisApiKey.Should().Be("FIS_API_KEY");
    }

    [Fact]
    public void KhipuReceiverId_ShouldHaveCorrectValue()
    {
        SecretTypes.KhipuReceiverId.Should().Be("KHIPU_RECEIVER_ID");
    }

    [Fact]
    public void KhipuSecret_ShouldHaveCorrectValue()
    {
        SecretTypes.KhipuSecret.Should().Be("KHIPU_SECRET");
    }

    [Theory]
    [InlineData(SecretTypes.KhipuWebhookSecret, "KHIPU_WEBHOOK_SECRET")]
    [InlineData(SecretTypes.GetnetLogin, "GETNET_LOGIN")]
    [InlineData(SecretTypes.GetnetSecretKey, "GETNET_SECRET_KEY")]
    [InlineData(SecretTypes.BciPagosAccountId, "BCIPAGOS_ACCOUNT_ID")]
    [InlineData(SecretTypes.BciPagosTokenSecret, "BCIPAGOS_TOKEN_SECRET")]
    [InlineData(SecretTypes.BciPagosUsername, "BCIPAGOS_USERNAME")]
    [InlineData(SecretTypes.BciPagosPassword, "BCIPAGOS_PASSWORD")]
    [InlineData(SecretTypes.BancoChileMerchantId, "BANCOCHILE_MERCHANT_ID")]
    [InlineData(SecretTypes.BancoChileSigningKey, "BANCOCHILE_SIGNING_KEY")]
    public void PaymentGatewaySecrets_ShouldHaveCorrectValues(string actual, string expected)
    {
        actual.Should().Be(expected);
    }

    [Fact]
    public void DbNetApiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.DbNetApiKey.Should().Be("DBNET_API_KEY");
    }

    [Fact]
    public void TrackingApiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.TrackingApiKey.Should().Be("TRACKING_API_KEY");
    }

    [Fact]
    public void SignerCertificate_ShouldHaveCorrectValue()
    {
        SecretTypes.SignerCertificate.Should().Be("SIGNER_CERTIFICATE");
    }

    [Fact]
    public void StorageAccessKey_ShouldHaveCorrectValue()
    {
        SecretTypes.StorageAccessKey.Should().Be("STORAGE_ACCESS_KEY");
    }
}
