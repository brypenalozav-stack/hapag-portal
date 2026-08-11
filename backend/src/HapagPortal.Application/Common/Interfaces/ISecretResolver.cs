namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Resuelve un secreto por tipo con precedencia de ámbito: si el cliente tiene una
/// credencial propia (Client) se usa; si no, cae a la del sistema (Global). Devuelve
/// el valor descifrado o null si no existe en ningún ámbito.
/// </summary>
public interface ISecretResolver
{
    Task<string?> ResolveAsync(string type, Guid? clientId, CancellationToken cancellationToken = default);
}
