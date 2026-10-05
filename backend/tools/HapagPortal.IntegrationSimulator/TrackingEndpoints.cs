namespace HapagPortal.IntegrationSimulator;

public sealed record EventLocation(string? UnLocode, string Name, string? CountryCode, string? Facility);

public sealed record TrackingEvent(
    string EventId,
    string EventType,
    string EventClassifier,
    string EventCode,
    string? Description,
    DateTime OccurredAt,
    EventLocation Location,
    string? EquipmentNumber,
    string? VesselName,
    string? Voyage,
    string? TransportMode);

public sealed record TrackingEvents(string Reference, string ReferenceType, DateTime LastUpdatedAt, IReadOnlyList<TrackingEvent> Events);

/// <summary>
/// CT-TRACK bajo <c>/tracking</c> (la ruta del contrato es <c>/tracking/{reference}/events</c>, así que la
/// URL completa es <c>/tracking/tracking/{reference}/events</c>). Como <c>DummyTrackingProvider</c>: las
/// referencias con prefijo "HLCU" tienen tres hitos fijos; el resto, ninguno.
/// </summary>
public static class TrackingEndpoints
{
    private static readonly DateTime LastUpdatedAt = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    public static void MapTracking(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tracking");
        group.MapGet("/tracking/{reference}/events", GetEvents);
    }

    private static IResult GetEvents(HttpContext context, string reference)
    {
        var errors = new RequestErrors();
        SimRequest.CheckLength(reference, "reference", errors, minLength: 4);
        var referenceType = SimRequest.CheckEnum(
            SimRequest.OptionalQuery(context, "referenceType"), "referenceType", errors, "BL", "BOOKING", "CONTAINER") ?? "BL";

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        var normalized = reference.Trim().ToUpperInvariant();
        var shanghai = new EventLocation("CNSHA", "Shanghai", "CN", null);

        IReadOnlyList<TrackingEvent> events = normalized.StartsWith("HLCU", StringComparison.Ordinal)
            ? [
                new TrackingEvent($"{normalized}-1", "EQUIPMENT", "ACT", "LOAD", "Contenedor cargado a bordo",
                    new DateTime(2026, 9, 19, 14, 0, 0, DateTimeKind.Utc), shanghai, "HLXU1234567", "SANTOS EXPRESS", "2609N", "VESSEL"),
                new TrackingEvent($"{normalized}-2", "TRANSPORT", "ACT", "DEPA", "Zarpe de la nave",
                    new DateTime(2026, 9, 20, 6, 0, 0, DateTimeKind.Utc), shanghai, null, "SANTOS EXPRESS", "2609N", "VESSEL"),
                new TrackingEvent($"{normalized}-3", "TRANSPORT", "EST", "ARRI", "Arribo estimado",
                    new DateTime(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc), new EventLocation("CLSAI", "San Antonio", "CL", null),
                    null, "SANTOS EXPRESS", "2609N", "VESSEL"),
            ]
            : [];

        return Results.Json(new TrackingEvents(reference, referenceType, LastUpdatedAt, events));
    }
}
