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

    [Fact]
    public void BancoChileApiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.BancoChileApiKey.Should().Be("BANCOCHILE_API_KEY");
    }

    [Fact]
    public void SantanderApiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.SantanderApiKey.Should().Be("SANTANDER_API_KEY");
    }

    [Fact]
    public void BciApiKey_ShouldHaveCorrectValue()
    {
        SecretTypes.BciApiKey.Should().Be("BCI_API_KEY");
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
