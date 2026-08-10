namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Resuelve el conjunto de permisos efectivos de un usuario a partir de sus roles.
/// Administrador / SuperAdmin (y el legado "Admin") obtienen todos los permisos.
/// </summary>
public interface IPermissionResolver
{
    Task<IReadOnlyList<string>> ResolveAsync(IEnumerable<string> roleCodes, CancellationToken cancellationToken = default);
}
