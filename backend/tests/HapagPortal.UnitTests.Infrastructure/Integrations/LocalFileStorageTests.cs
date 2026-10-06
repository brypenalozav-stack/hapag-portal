namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Infrastructure.Integrations.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Almacenamiento local (CT-STORAGE, <c>Mode=Local</c>): persiste entre instancias, respeta la regla de claves
/// y rechaza cualquier clave o contenedor que pudiera salir de la carpeta raíz.
/// </summary>
public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"hapag-storage-{Guid.NewGuid():N}");

    private LocalFileStorage NewStorage() => new(_root, Substitute.For<ILogger<LocalFileStorage>>());

    [Fact]
    public async Task Save_ShouldPersistUnderTheRoot_AndBeReadableByANewInstance()
    {
        using var content = new MemoryStream([1, 2, 3, 4]);

        var key = (await NewStorage().SaveAsync(content, "../../evil name.pdf", "application/pdf", "shipment-documents")).Value;
        var opened = await NewStorage().OpenReadAsync(key);

        key.Should().MatchRegex(@"^shipment-documents/\d{4}/\d{2}/[0-9a-f]{32}$");
        key.Should().NotContain("evil");
        File.Exists(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue();
        Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();

        await using var stream = opened.Value!;
        using var read = new MemoryStream();
        await stream.CopyToAsync(read);
        read.ToArray().Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task UnknownValidKey_ShouldReturnNull()
    {
        var result = await NewStorage().OpenReadAsync($"shipment-documents/2026/10/{Guid.NewGuid():N}");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("shipment-documents/2026/10/../../../../outside")]
    [InlineData("shipment-documents/../../outside/0123456789abcdef0123456789abcdef")]
    [InlineData("..\\..\\windows\\win.ini")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("shipment-documents/2026/10/0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("")]
    public async Task KeysOutsideTheContract_ShouldBeRejected(string key)
    {
        var storage = NewStorage();

        var read = await storage.OpenReadAsync(key);
        var delete = await storage.DeleteAsync(key);

        read.IsFailure.Should().BeTrue();
        read.Error.Code.Should().Be("Error.Validation");
        delete.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("../documents")]
    [InlineData("a/b")]
    [InlineData("Documents")]
    [InlineData("")]
    public async Task InvalidContainers_ShouldBeRejected(string container)
    {
        using var content = new MemoryStream([1]);

        var result = await NewStorage().SaveAsync(content, "a.pdf", "application/pdf", container);

        result.IsFailure.Should().BeTrue();
        Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheFile_AndIgnoreAMissingOne()
    {
        var storage = NewStorage();
        using var content = new MemoryStream([9]);
        var key = (await storage.SaveAsync(content, "a.pdf", "application/pdf", "shipment-documents")).Value;

        (await storage.DeleteAsync(key)).IsSuccess.Should().BeTrue();
        (await storage.DeleteAsync(key)).IsSuccess.Should().BeTrue();
        (await storage.OpenReadAsync(key)).Value.Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
