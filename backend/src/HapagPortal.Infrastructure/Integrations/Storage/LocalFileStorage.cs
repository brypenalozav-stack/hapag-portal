using System.Text.RegularExpressions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Storage;

/// <summary>
/// Almacenamiento en el sistema de archivos local (CT-STORAGE, <c>Integrations:Storage:Mode=Local</c>) para
/// que los documentos persistan entre reinicios en desarrollo y staging. Raíz: <c>Integrations:Storage:LocalPath</c>
/// (por defecto <c>&lt;LocalApplicationData&gt;/HapagPortal/storage</c>). Misma regla de claves que el contrato
/// (<c>&lt;contenedor&gt;/&lt;aaaa&gt;/&lt;mm&gt;/&lt;uuid&gt;</c>): el nombre original nunca forma parte de la ruta
/// y toda clave se valida contra el patrón y contra la raíz, de modo que no hay recorrido de directorios.
/// </summary>
public sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(string rootPath, ILogger<LocalFileStorage> logger)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("The local storage path is required.", nameof(rootPath));

        _root = Path.GetFullPath(rootPath);
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public static string DefaultRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HapagPortal", "storage");

    public async Task<Result<string>> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string container,
        CancellationToken cancellationToken = default)
    {
        if (!ContainerPattern().IsMatch(container))
            return Result<string>.Failure(Error.Validation($"Invalid storage container '{container}'."));

        var now = DateTime.UtcNow;
        var key = $"{container}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
        var path = PathOf(key)!;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Se escribe en un temporal y se mueve: un lector nunca ve un archivo a medio escribir.
        var temporary = path + ".tmp";
        await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        File.Move(temporary, path);

        _logger.LogInformation(
            "Archivo guardado (local) - Key: {Key}, FileName: {FileName}, ContentType: {ContentType}",
            key, fileName, contentType);

        return Result<string>.Success(key);
    }

    public Task<Result<Stream?>> OpenReadAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var path = PathOf(key);
        if (path is null)
            return Task.FromResult(Result<Stream?>.Failure(InvalidKey(key)));

        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;

        return Task.FromResult(Result<Stream?>.Success(stream));
    }

    public Task<Result> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var path = PathOf(key);
        if (path is null)
            return Task.FromResult(Result.Failure(InvalidKey(key)));

        if (File.Exists(path))
            File.Delete(path);

        return Task.FromResult(Result.Success());
    }

    /// <summary>Ruta absoluta de una clave válida dentro de la raíz; nula si la clave no cumple el contrato.</summary>
    private string? PathOf(string key)
    {
        if (string.IsNullOrEmpty(key) || !KeyPattern().IsMatch(key))
            return null;

        var path = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;

        return path.StartsWith(rootWithSeparator, StringComparison.Ordinal) ? path : null;
    }

    private static Error InvalidKey(string key) => Error.Validation($"Invalid storage key '{key}'.");

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,62}$")]
    private static partial Regex ContainerPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,62}/[0-9]{4}/[0-9]{2}/[0-9a-f]{32}$")]
    private static partial Regex KeyPattern();
}
