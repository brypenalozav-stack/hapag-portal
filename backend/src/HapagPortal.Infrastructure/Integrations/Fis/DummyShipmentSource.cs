using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Fis;

/// <summary>
/// Embarques simulados de FIS/Data Lake (CT-FIS). Determinista: tres BL fijos, uno por RUT de escenario
/// (exento, crédito y FFWW). BL desconocido: <c>Success(null)</c>.
/// </summary>
public sealed class DummyShipmentSource(ILogger<DummyShipmentSource> logger) : IShipmentSource
{
    private static readonly (DateTime UpdatedAt, ShipmentRecord Record)[] Shipments =
    [
        (new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            new ShipmentRecord("HLCUSCL2609A1234", "BKG2609001", "IMPORT", "SANTOS EXPRESS", "2609N",
                new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                7, "SAAM", null, "MC000101", "76000001-1")),
        (new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc),
            new ShipmentRecord("HLCUSCL2609A5678", "BKG2609002", "IMPORT", "SANTOS EXPRESS", "2609N",
                new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                10, "SITRANS", null, "MC000202", "76000002-2")),
        (new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
            new ShipmentRecord("HLCUVAP2610E0001", "BKG2610003", "EXPORT", "VALPARAISO EXPRESS", "2610S",
                new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
                null, null, "SITRANS", "MC000303", "76000003-3")),
    ];

    public Task<Result<ShipmentRecord?>> GetByBlNumberAsync(
        string blNumber,
        CancellationToken cancellationToken = default)
    {
        var record = Shipments
            .Select(s => s.Record)
            .FirstOrDefault(r => string.Equals(r.BlNumber, blNumber?.Trim(), StringComparison.OrdinalIgnoreCase));

        logger.LogDebug(
            "Embarque FIS (dummy) - BL: {BlNumber}, Found: {Found}",
            blNumber, record is not null);

        return Task.FromResult(Result<ShipmentRecord?>.Success(record));
    }

    public Task<Result<IReadOnlyList<ShipmentRecord>>> GetUpdatedSinceAsync(
        DateTime sinceUtc,
        string? shipmentType,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ShipmentRecord> records = Shipments
            .Where(s => s.UpdatedAt >= sinceUtc
                && (shipmentType is null || string.Equals(s.Record.ShipmentType, shipmentType, StringComparison.OrdinalIgnoreCase)))
            .Select(s => s.Record)
            .ToList();

        logger.LogDebug(
            "Embarques FIS (dummy) - Since: {Since}, Type: {Type}, Items: {Count}",
            sinceUtc, shipmentType, records.Count);

        return Task.FromResult(Result<IReadOnlyList<ShipmentRecord>>.Success(records));
    }
}
