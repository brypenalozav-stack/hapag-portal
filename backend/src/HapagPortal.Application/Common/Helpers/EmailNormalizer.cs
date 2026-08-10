namespace HapagPortal.Application.Common.Helpers;

/// <summary>
/// Normaliza direcciones de correo para comparaciones y persistencia consistentes.
/// Evita cuentas duplicadas por diferencias de mayúsculas/espacios (BUG-9).
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();
}
