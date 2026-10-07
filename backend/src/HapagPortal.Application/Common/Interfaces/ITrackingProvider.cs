using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Hitos de un embarque (CT-TRACK, <c>GET /tracking/{reference}/events</c>). Sin eventos: lista vacía.
/// </summary>
public interface ITrackingProvider
{
    Task<Result<IReadOnlyList<TrackingEvent>>> GetEventsAsync(
        string reference,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Evento de seguimiento. <c>EventType</c>: SHIPMENT, TRANSPORT o EQUIPMENT;
/// <c>EventClassifier</c>: PLN, EST o ACT.
/// </summary>
public sealed record TrackingEvent(
    string EventId,
    string EventType,
    string EventClassifier,
    string EventCode,
    string? Description,
    DateTime OccurredAt,
    string LocationName,
    string? UnLocode,
    string? EquipmentNumber,
    string? VesselName,
    string? Voyage);
