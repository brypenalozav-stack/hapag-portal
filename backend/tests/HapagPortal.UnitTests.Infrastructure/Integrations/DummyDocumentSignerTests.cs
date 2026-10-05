namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.Integrations.Signature;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class DummyDocumentSignerTests
{
    private readonly DummyDocumentSigner _signer = new(Substitute.For<ILogger<DummyDocumentSigner>>());

    [Fact]
    public async Task SignAsync_ShouldReturnSameContentWithSimpleSignature()
    {
        var content = "%PDF-1.7 dummy"u8.ToArray();

        var result = await _signer.SignAsync(new SignDocumentRequest(content, "TRANSSHIPMENT_CERTIFICATE", "default"));

        result.IsSuccess.Should().BeTrue();
        result.Value.SignedContent.Should().Equal(content);
        result.Value.SignatureLevel.Should().Be("SIMPLE");
        result.Value.SignatureId.Should().StartWith("DUMMY-SIG-");
    }

    [Fact]
    public async Task SignAsync_SameContent_ShouldReturnSameSignatureId()
    {
        var content = "%PDF-1.7 dummy"u8.ToArray();

        var first = await _signer.SignAsync(new SignDocumentRequest(content, "FREIGHT_CERTIFICATE", "default"));
        var second = await _signer.SignAsync(new SignDocumentRequest(content, "FREIGHT_CERTIFICATE", "default"));
        var other = await _signer.SignAsync(new SignDocumentRequest("%PDF-1.7 other"u8.ToArray(), "FREIGHT_CERTIFICATE", "default"));

        second.Value.SignatureId.Should().Be(first.Value.SignatureId);
        other.Value.SignatureId.Should().NotBe(first.Value.SignatureId);
    }
}
