namespace HapagPortal.IntegrationSimulator;

/// <summary>
/// Escenarios de datos del simulador: los mismos que los adaptadores Dummy de la Fase 6b
/// (<c>HapagPortal.Infrastructure.Integrations.*.Dummy*</c>). El simulador no referencia Infrastructure
/// para no arrastrar sus dependencias; si cambia un escenario Dummy, se cambia también aquí.
/// </summary>
public static class SimulatorData
{
    // Nexus (DummyNexusData).
    public const string ExemptTaxId = "76000001-1";
    public const string CreditTaxId = "76000002-2";
    public const string FreightForwarderTaxId = "76000003-3";

    /// <summary>Cuenta de Bolivia exceptuada del cargo XOM (Ola G, M3-10).</summary>
    public const string XomExemptTaxId = "1029384756";
    public const string NexusSource = "DUMMY";
    public static readonly DateOnly ValidFrom = new(2026, 1, 1);
    public static readonly DateOnly TariffValidFrom = new(2026, 10, 1);

    // Tipos de cambio con base USD (DummyExchangeRateProvider).
    public static readonly IReadOnlyDictionary<string, decimal> UnitsPerUsd = new Dictionary<string, decimal>(StringComparer.Ordinal)
    {
        ["USD"] = 1m,
        ["CLP"] = 950m,
        ["BOB"] = 6.91m,
        ["EUR"] = 0.92m,
    };

    // FIS (DummyShipmentSource): un BL por RUT de escenario.
    public static readonly IReadOnlyList<ShipmentSummary> Shipments =
    [
        new("HLCUSCL2609A1234", "BKG2609001", "IMPORT", "SANTOS EXPRESS", "2609N",
            Utc(2026, 9, 20), Utc(2026, 10, 15), 7, "SAAM", null, "MC000101", ExemptTaxId, Utc(2026, 10, 1, 12)),
        new("HLCUSCL2609A5678", "BKG2609002", "IMPORT", "SANTOS EXPRESS", "2609N",
            Utc(2026, 9, 20), Utc(2026, 10, 15), 10, "SITRANS", null, "MC000202", CreditTaxId, Utc(2026, 10, 5, 12)),
        new("HLCUVAP2610E0001", "BKG2610003", "EXPORT", "VALPARAISO EXPRESS", "2610S",
            Utc(2026, 10, 25), Utc(2026, 11, 20), null, null, "SITRANS", "MC000303", FreightForwarderTaxId, Utc(2026, 10, 10, 12)),
    ];

    public static bool IsTaxId(string? taxId, string expected) =>
        string.Equals(taxId?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private static DateTime Utc(int year, int month, int day, int hour = 0) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
