using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Tracking;

/// <summary>
/// Seguimiento simulado (CT-TRACK). Determinista: las referencias Hapag-Lloyd (prefijo "HLCU") devuelven
/// tres hitos fijos (carga, zarpe y arribo estimado); cualquier otra, una lista vacía.
/// </summary>
public sealed class DummyTrackingProvider(ILogger<DummyTrackingProvider> logger) : ITrackingProvider
{
    public Task<Result<IReadOnlyList<TrackingEvent>>> GetEventsAsync(
        string reference,
        CancellationToken cancellationToken = default)
    {
        var normalized = reference?.Trim().ToUpperInvariant() ?? string.Empty;

        IReadOnlyList<TrackingEvent> events = normalized.StartsWith("HLCU", StringComparison.Ordinal)
            ? [
                new TrackingEvent($"{normalized}-1", "EQUIPMENT", "ACT", "LOAD", "Contenedor cargado a bordo",
                    new DateTime(2026, 9, 19, 14, 0, 0, DateTimeKind.Utc), "Shanghai", "CNSHA", "HLXU1234567", "SANTOS EXPRESS", "2609N"),
                new TrackingEvent($"{normalized}-2", "TRANSPORT", "ACT", "DEPA", "Zarpe de la nave",
                    new DateTime(2026, 9, 20, 6, 0, 0, DateTimeKind.Utc), "Shanghai", "CNSHA", null, "SANTOS EXPRESS", "2609N"),
                new TrackingEvent($"{normalized}-3", "TRANSPORT", "EST", "ARRI", "Arribo estimado",
                    new DateTime(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc), "San Antonio", "CLSAI", null, "SANTOS EXPRESS", "2609N"),
            ]
            : [];

        logger.LogDebug(
            "Tracking (dummy) - Reference: {Reference}, Events: {Count}",
            reference, events.Count);

        return Task.FromResult(Result<IReadOnlyList<TrackingEvent>>.Success(events));
    }
}
