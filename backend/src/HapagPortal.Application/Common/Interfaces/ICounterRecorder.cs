using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Counter Bolivia/Ultramar en Nexus (CT-COUNTER, M8-09): registra por BL la fecha de canje, la recepción del HBL y la
/// marca de desconsolidado, y lee el estado vigente en el origen. Las fallas del sistema externo vuelven como
/// <c>DomainErrors.Integration.*</c>; un BL sin registro en el origen se lee como nulo.
/// </summary>
public interface ICounterRecorder
{
    Task<Result<CounterSourceRecord>> RecordAsync(
        CounterRecordRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<CounterSourceRecord?>> GetAsync(
        string blNumber,
        CancellationToken cancellationToken = default);
}

/// <summary>Datos de Counter a registrar para un BL; <c>RecordedBy</c> es el usuario interno del portal.</summary>
public sealed record CounterRecordRequest(
    string BlNumber,
    string Country,
    DateOnly? ExchangeDate,
    bool HblReceived,
    DateOnly? HblReceivedAt,
    bool Deconsolidated,
    DateOnly? DeconsolidatedAt,
    string? Notes,
    string RecordedBy);

/// <summary>Registro de Counter tal como queda en Nexus, con su referencia en el origen.</summary>
public sealed record CounterSourceRecord(
    string BlNumber,
    string Country,
    DateOnly? ExchangeDate,
    bool HblReceived,
    DateOnly? HblReceivedAt,
    bool Deconsolidated,
    DateOnly? DeconsolidatedAt,
    string SourceReference,
    DateTime RecordedAt,
    string? RecordedBy);
