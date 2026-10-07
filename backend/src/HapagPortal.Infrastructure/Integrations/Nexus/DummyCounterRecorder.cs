using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Counter de Nexus simulado (CT-COUNTER, M8-09). Guarda en memoria lo registrado (la misma clave de idempotencia
/// devuelve el mismo registro) y conoce el estado de los BL de demostración sembrados. Un BL cuyo número contiene
/// <c>NXFAIL</c> simula que Nexus no está disponible, para ejercitar la sincronización pendiente y su reintento.
/// </summary>
public sealed class DummyCounterRecorder(ILogger<DummyCounterRecorder> logger) : ICounterRecorder
{
    public const string FailureMarker = "NXFAIL";

    private readonly ConcurrentDictionary<string, CounterSourceRecord> _records = new(StringComparer.OrdinalIgnoreCase)
    {
        // BL de Bolivia de la demo (BL08): canje y HBL registrados, todavía sin desconsolidar.
        ["HLCUARI260300830"] = new CounterSourceRecord(
            "HLCUARI260300830", "BO", new DateOnly(2026, 10, 2), true, new DateOnly(2026, 10, 3), false, null,
            "CNT-BO-000830", new DateTime(2026, 10, 3, 14, 0, 0, DateTimeKind.Utc), "admin@hapag-lloyd.cl"),
    };

    private readonly ConcurrentDictionary<string, CounterSourceRecord> _byIdempotencyKey = new(StringComparer.Ordinal);

    public Task<Result<CounterSourceRecord>> RecordAsync(
        CounterRecordRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (request.BlNumber.Contains(FailureMarker, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result<CounterSourceRecord>.Failure(DomainErrors.Integration.Unavailable("Nexus")));

        var record = _byIdempotencyKey.GetOrAdd(idempotencyKey, _ => new CounterSourceRecord(
            request.BlNumber, request.Country, request.ExchangeDate, request.HblReceived, request.HblReceivedAt,
            request.Deconsolidated, request.DeconsolidatedAt,
            $"CNT-{request.Country}-{(idempotencyKey.Length > 8 ? idempotencyKey[^8..] : idempotencyKey).ToUpperInvariant()}",
            DateTime.UtcNow, request.RecordedBy));
        _records[request.BlNumber] = record;

        logger.LogDebug("Counter (dummy) - BL: {BlNumber}, Reference: {Reference}", request.BlNumber, record.SourceReference);
        return Task.FromResult(Result<CounterSourceRecord>.Success(record));
    }

    public Task<Result<CounterSourceRecord?>> GetAsync(string blNumber, CancellationToken cancellationToken = default)
    {
        if (blNumber.Contains(FailureMarker, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result<CounterSourceRecord?>.Failure(DomainErrors.Integration.Unavailable("Nexus")));

        return Task.FromResult(Result<CounterSourceRecord?>.Success(_records.GetValueOrDefault(blNumber.Trim())));
    }
}
