using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Tracking;

/// <summary>
/// Cliente Real de seguimiento (CT-TRACK, <c>GET /tracking/{reference}/events</c>). Referencia sin
/// eventos o inexistente (404): lista vacía.
/// </summary>
public sealed class HttpTrackingProvider(HttpClient httpClient, ISecretResolver secretResolver) : ITrackingProvider
{
    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.Tracking, SecretTypes.TrackingApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public async Task<Result<IReadOnlyList<TrackingEvent>>> GetEventsAsync(
        string reference,
        CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<TrackingEventsDto>(
            httpClient, secretResolver, Endpoint, "getTrackingEvents", HttpMethod.Get,
            $"tracking/{IntegrationHttp.Segment(reference)}/events", cancellationToken);

        if (result.IsFailure)
            return Result<IReadOnlyList<TrackingEvent>>.Failure(result.Error);

        IReadOnlyList<TrackingEvent> events = result.Value?.Events
            .Select(e => new TrackingEvent(
                e.EventId,
                e.EventType,
                e.EventClassifier,
                e.EventCode,
                e.Description,
                e.OccurredAt,
                e.Location.Name,
                e.Location.UnLocode,
                e.EquipmentNumber,
                e.VesselName,
                e.Voyage))
            .ToList() ?? [];

        return Result<IReadOnlyList<TrackingEvent>>.Success(events);
    }

    private sealed record TrackingEventsDto(
        string Reference,
        string ReferenceType,
        DateTime LastUpdatedAt,
        IReadOnlyList<TrackingEventDto> Events);

    private sealed record TrackingEventDto(
        string EventId,
        string EventType,
        string EventClassifier,
        string EventCode,
        DateTime OccurredAt,
        LocationDto Location,
        string? Description = null,
        string? EquipmentNumber = null,
        string? VesselName = null,
        string? Voyage = null);

    private sealed record LocationDto(string Name, string? UnLocode = null);
}
