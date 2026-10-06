namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Escenarios por RUT compartidos por los adaptadores Dummy de Nexus (y por el simulador de la Fase 6c):
/// <c>76000001-1</c> exento de Gate In y EDS, <c>76000002-2</c> con crédito a 30 días,
/// <c>76000003-3</c> FFWW, NIT <c>1029384756</c> con excepción de administración de contenedor (XOM, M3-10);
/// cualquier otro RUT, sin condiciones.
/// </summary>
public static class DummyNexusData
{
    public const string ExemptTaxId = "76000001-1";
    public const string CreditTaxId = "76000002-2";
    public const string FreightForwarderTaxId = "76000003-3";

    /// <summary>Cuenta de Bolivia exceptuada del cargo XOM (Ola G, M3-10).</summary>
    public const string XomExemptTaxId = "1029384756";

    public const string Source = "DUMMY";

    /// <summary>Inicio de vigencia de todas las condiciones simuladas.</summary>
    public static readonly DateOnly ValidFrom = new(2026, 1, 1);

    public static bool IsTaxId(string taxId, string expected) =>
        string.Equals(taxId?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
