using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;

namespace HapagPortal.Infrastructure.Integrations.Fis;

/// <summary>
/// Embarques de demostración del portal tal como los informaría FIS (CT-FIS), con los campos propuestos en la
/// Ola F: UN/LOCODE del puerto de descarga y del destino final, DIFU (M2-01) y documento de transporte con su
/// estado de emisión (M2-02). Lo usan el adaptador Dummy y la semilla, que guarda en cada BL el último estado
/// conocido, de modo que el listado y la consulta en el momento coincidan. Casos: BL con telex release, SWB
/// emitido, BL entregado (surrender), emisión autorizada en destino (Bolivia), emitido en destino, EBL de Wave
/// pendiente, emitido y transferido, y dos BL con destino final distinto del puerto de descarga: Antofagasta sin
/// DIFU (no se publica) y Punta Arenas con DIFU asociado (se publica).
/// </summary>
public static class DummyFisDemoData
{
    private static DateTime Utc(int year, int month, int day, int hour = 0) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    public static readonly IReadOnlyList<ShipmentRecord> Records =
    [
        new("HLCUVAL250100123", "HLCUBKG2501001", "IMPORT", "Hamburg Express", "025E", Utc(2026, 3, 1), Utc(2026, 4, 5), null, null, null, null, null,
            "CLSAI", "CLSCL", null, null, "BL", null, BlIssuanceSourceStatuses.TelexReleased, Utc(2026, 4, 2, 14), "CNSHA"),
        new("HLCUVAL250200456", "HLCUBKG2502004", "IMPORT", "Berlin Express", "031W", Utc(2026, 4, 15), Utc(2026, 5, 20), null, null, null, null, null,
            "CLVAP", "CLVAP", null, null, "SWB", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 4, 16, 9), "KRPUS"),
        new("HLCUVAL250300789", "HLCUBKG2503007", "IMPORT", "Colombo Express", "018E", Utc(2026, 1, 10), Utc(2026, 2, 15), null, null, null, null, null,
            "CLSAI", "CLSCL", null, null, "BL", null, BlIssuanceSourceStatuses.Surrendered, Utc(2026, 2, 16, 12), "CLSAI"),
        new("HLCUARI260100045", "HLCUBKG2601045", "IMPORT", "Antofagasta Express", "012E", Utc(2026, 2, 20), Utc(2026, 3, 28), null, null, null, null, null,
            "CLARI", "BOLPB", null, null, "BL", null, BlIssuanceSourceStatuses.DestinationIssuanceAuthorized, Utc(2026, 3, 20, 15), "BOLPB"),
        new("HLCUIQQ260200078", "HLCUBKG2602078", "IMPORT", "Guayaquil Express", "007W", Utc(2026, 4, 1), Utc(2026, 5, 10), null, null, null, null, null,
            "CLIQQ", "BOSRZ", null, null, "BL", null, BlIssuanceSourceStatuses.IssuedAtDestination, Utc(2026, 5, 8, 13), "BOSRZ"),
        new("HLCUSAI260300610", "HLCUBKG2603061", "EXPORT", "Valparaiso Express", "2610S", Utc(2026, 10, 20), Utc(2026, 11, 25), null, null, null, null, null,
            "NLRTM", "NLRTM", null, null, "EBL", EblPlatforms.Wave, BlIssuanceSourceStatuses.NotIssued, null, null),
        new("HLCUVAP260300720", "HLCUBKG2603072", "EXPORT", "Santos Express", "2611N", Utc(2026, 10, 28), Utc(2026, 12, 2), null, null, null, null, null,
            "CNSHA", "CNSHA", null, null, "EBL", EblPlatforms.Wave, BlIssuanceSourceStatuses.Issued, Utc(2026, 10, 29, 10), "CLVAP"),
        new("HLCUARI260300830", "HLCUBKG2603083", "EXPORT", "Antofagasta Express", "2612S", Utc(2026, 11, 5), Utc(2026, 11, 12), null, null, null, null, null,
            "PECLL", "PELIM", null, null, "SWB", null, BlIssuanceSourceStatuses.Draft, null, null),
        new("HLCUSAI260400910", "HLCUBKG2604091", "IMPORT", "Rio de Janeiro Express", "2608E", Utc(2026, 7, 15), Utc(2026, 8, 20), null, null, null, null, null,
            "CLSAI", "CLSCL", null, null, "BL", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 7, 16, 11), "DEHAM"),
        new("HLCUSAI260401020", "HLCUBKG2604102", "IMPORT", "Valparaiso Express", "2609E", Utc(2026, 8, 20), Utc(2026, 9, 28), null, null, null, null, null,
            "CLSAI", "CLSCL", null, null, "SWB", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 8, 21, 10), "CNNGB"),
        new("HLCUVAP260401130", "HLCUBKG2604113", "IMPORT", "Santos Express", "2609N", Utc(2026, 8, 25), Utc(2026, 9, 15), null, null, null, null, null,
            "CLVAP", "CLVAP", null, null, "EBL", EblPlatforms.Wave, BlIssuanceSourceStatuses.Transferred, Utc(2026, 9, 1, 16), "BRSSZ"),
        new("HLCUSAI260501240", "HLCUBKG2605124", "IMPORT", "Cartagena Express", "2611E", Utc(2026, 8, 28), Utc(2026, 10, 2), null, null, null, null, null,
            "CLSAI", "CLSCL", null, null, "BL", null, BlIssuanceSourceStatuses.TelexReleased, Utc(2026, 9, 30, 18), "JPYOK"),
        new("HLCUVAP260501350", "HLCUBKG2605135", "IMPORT", "Callao Express", "2611N", Utc(2026, 8, 30), Utc(2026, 10, 1), null, null, null, null, null,
            "CLVAP", "CLVAP", null, null, "BL", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 8, 31, 9), "CNSHA"),
        new("HLCUSAI260601410", "HLCUBKG2606141", "IMPORT", "Lima Express", "2612E", Utc(2026, 9, 1), Utc(2026, 10, 3), null, null, null, null, null,
            "CLSAI", "CLANF", null, null, "BL", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 9, 2, 12), "CNSHA"),
        new("HLCUSAI260601520", "HLCUBKG2606152", "IMPORT", "Lima Express", "2612E", Utc(2026, 9, 1), Utc(2026, 10, 3), null, null, null, null, null,
            "CLSAI", "CLPUQ", "PUQ-DIFU-0915", "CLPUQ", "SWB", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 9, 2, 12), "CNSHA"),
        // Fase 2 Ola G: exportaciones ya zarpadas de Chile y Bolivia (servicios on demand de exportación, M2-04).
        new("HLCUSAI260901610", "HLCUBKG2609161", "EXPORT", "Cartagena Express", "2609S", Utc(2026, 9, 28), Utc(2026, 10, 10), null, null, null, null, null,
            "PECLL", "PELIM", null, null, "BL", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 9, 28, 6), "CLSAI"),
        new("HLCUARI260901720", "HLCUBKG2609172", "EXPORT", "Antofagasta Express", "2609S", Utc(2026, 9, 30), Utc(2026, 10, 7), null, null, null, null, null,
            "PECLL", "PELIM", null, null, "SWB", null, BlIssuanceSourceStatuses.Issued, Utc(2026, 9, 30, 8), "CLARI"),
    ];

    public static ShipmentRecord? Find(string? blNumber) =>
        Records.FirstOrDefault(r => string.Equals(r.BlNumber, blNumber?.Trim(), StringComparison.OrdinalIgnoreCase));
}
