using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Almacenamiento de archivos (CT-STORAGE). La clave tiene la forma
/// <c>&lt;contenedor&gt;/&lt;aaaa&gt;/&lt;mm&gt;/&lt;uuid&gt;</c>; el nombre original no forma parte de la clave.
/// </summary>
public interface IFileStorage
{
    /// <summary>Guarda el contenido y devuelve la clave del archivo.</summary>
    Task<Result<string>> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string container,
        CancellationToken cancellationToken = default);

    /// <summary>Abre el archivo para lectura; <c>Success(null)</c> si la clave no existe.</summary>
    Task<Result<Stream?>> OpenReadAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Borra el archivo; borrar una clave inexistente no es error.</summary>
    Task<Result> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);
}
