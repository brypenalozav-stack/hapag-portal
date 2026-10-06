namespace HapagPortal.Application.Common.Helpers;

/// <summary>
/// Normaliza RUT/NIT al formato de CT-NEXUS (<c>12345678-9</c>, sin puntos ni espacios y en mayúsculas),
/// para consultar Nexus y comparar cuentas con independencia de cómo se registraron.
/// </summary>
public static class TaxIdNormalizer
{
    public static string Normalize(string? taxId) =>
        new string((taxId ?? string.Empty).Where(c => c is not ('.' or ' ')).ToArray()).Trim().ToUpperInvariant();

    public static bool AreEqual(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && Normalize(left) == Normalize(right);
}
