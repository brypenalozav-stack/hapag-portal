namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Infrastructure.Integrations.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class DummyFileStorageTests
{
    private readonly DummyFileStorage _storage = new(Substitute.For<ILogger<DummyFileStorage>>());

    [Fact]
    public async Task SaveAsync_ShouldReturnKeyWithContainerYearMonthAndId()
    {
        using var content = new MemoryStream([1, 2, 3]);

        var result = await _storage.SaveAsync(content, "comprobante.pdf", "application/pdf", "comprobantes");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().MatchRegex(@"^comprobantes/\d{4}/\d{2}/[0-9a-f]{32}$");
        result.Value.Should().NotContain("comprobante.pdf");
    }

    [Fact]
    public async Task OpenReadAsync_SavedKey_ShouldReturnSameContent()
    {
        using var content = new MemoryStream([1, 2, 3]);
        var key = (await _storage.SaveAsync(content, "a.pdf", "application/pdf", "documentos")).Value;

        var result = await _storage.OpenReadAsync(key);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        using var read = new MemoryStream();
        await result.Value!.CopyToAsync(read);
        read.ToArray().Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task OpenReadAsync_UnknownKey_ShouldReturnSuccessWithNull()
    {
        var result = await _storage.OpenReadAsync("documentos/2026/10/unknown");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        using var content = new MemoryStream([1]);
        var key = (await _storage.SaveAsync(content, "a.pdf", "application/pdf", "registro")).Value;

        var deleted = await _storage.DeleteAsync(key);
        var read = await _storage.OpenReadAsync(key);

        deleted.IsSuccess.Should().BeTrue();
        read.Value.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_UnknownKey_ShouldSucceed()
    {
        var result = await _storage.DeleteAsync("registro/2026/10/unknown");

        result.IsSuccess.Should().BeTrue();
    }
}
