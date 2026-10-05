using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Storage;

/// <summary>
/// Almacenamiento simulado en memoria (CT-STORAGE). Se pierde al reiniciar la API: solo para
/// desarrollo y pruebas. La clave sigue la regla del contrato: <c>&lt;contenedor&gt;/&lt;aaaa&gt;/&lt;mm&gt;/&lt;uuid&gt;</c>.
/// </summary>
public sealed class DummyFileStorage(ILogger<DummyFileStorage> logger) : IFileStorage
{
    private readonly ConcurrentDictionary<string, StoredFile> _files = new(StringComparer.Ordinal);

    public async Task<Result<string>> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string container,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var now = DateTime.UtcNow;
        var key = $"{container}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
        _files[key] = new StoredFile(buffer.ToArray(), fileName, contentType);

        logger.LogInformation(
            "Archivo guardado (dummy) - Key: {Key}, FileName: {FileName}, ContentType: {ContentType}, Bytes: {Bytes}",
            key, fileName, contentType, buffer.Length);

        return Result<string>.Success(key);
    }

    public Task<Result<Stream?>> OpenReadAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        Stream? stream = _files.TryGetValue(key, out var file)
            ? new MemoryStream(file.Content, writable: false)
            : null;

        return Task.FromResult(Result<Stream?>.Success(stream));
    }

    public Task<Result> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        _files.TryRemove(key, out _);
        return Task.FromResult(Result.Success());
    }

    private sealed record StoredFile(byte[] Content, string FileName, string ContentType);
}
