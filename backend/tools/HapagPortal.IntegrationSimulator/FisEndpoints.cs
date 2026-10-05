using System.Globalization;

namespace HapagPortal.IntegrationSimulator;

public sealed record ShipmentSummary(
    string BlNumber,
    string? BookingNumber,
    string ShipmentType,
    string? Vessel,
    string? Voyage,
    DateTime? Etd,
    DateTime? Eta,
    int? FreeDays,
    string? DepotImport,
    string? DepotExport,
    string? MatchCode,
    string? TaxId,
    DateTime UpdatedAt);

public sealed record ShipmentPage(IReadOnlyList<ShipmentSummary> Items, string? NextCursor);

public sealed record RouteLeg(
    string LegId,
    string? LegType,
    int SequenceNumber,
    string LocationCode,
    string? LocationName,
    string? CountryCode,
    string? VesselName,
    string? ScheduleVoyage,
    DateTime? Eta,
    DateTime? Etd);

public sealed record Equipment(string EquipmentNumber, string? EquipmentType, int? CargoLineNumber, int? ItemLineNumber);

public sealed record Party(string Function, string MatchCode, string? Name, string? TaxId);

public sealed record Revenue(string? EquipmentNumber, string RevenueCode, string? RevenueName, decimal RevenueValue, string RevenueCurrency);

public sealed record VesselCall(string VesselName, string? Imo, string? Ssy, DateTime? ArrivalConfirmation);

public sealed record FreeTime(int FreeDays, DateOnly? From, DateOnly? To, IReadOnlyList<object> Periods);

/// <summary>Detalle de CT-FIS: <c>ShipmentSummary</c> más ruta, contenedores, partes, ingresos, nave y días libres.</summary>
public sealed record Shipment(
    string BlNumber,
    string? BookingNumber,
    string ShipmentType,
    string? Vessel,
    string? Voyage,
    DateTime? Etd,
    DateTime? Eta,
    int? FreeDays,
    string? DepotImport,
    string? DepotExport,
    string? MatchCode,
    string? TaxId,
    DateTime UpdatedAt,
    IReadOnlyList<RouteLeg> Route,
    IReadOnlyList<Equipment> Equipments,
    IReadOnlyList<Party> Parties,
    IReadOnlyList<Revenue> Revenues,
    VesselCall? VesselCall,
    FreeTime? FreeTime);

/// <summary>CT-FIS bajo <c>/fis</c>.</summary>
public static class FisEndpoints
{
    private const int DefaultLimit = 100;

    public static void MapFis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fis");
        group.MapGet("/shipments", GetUpdatedSince);
        group.MapGet("/shipments/{blNumber}", GetByBlNumber);
    }

    private static IResult GetUpdatedSince(HttpContext context)
    {
        var errors = new RequestErrors();
        var since = SimRequest.ParseDateTime(SimRequest.RequiredQuery(context, "updatedSince", errors), "updatedSince", errors);
        var type = SimRequest.CheckEnum(SimRequest.OptionalQuery(context, "type"), "type", errors, "IMPORT", "EXPORT");
        var offset = SimRequest.ParseInt(SimRequest.OptionalQuery(context, "cursor"), "cursor", errors, 0, int.MaxValue) ?? 0;
        var limit = SimRequest.ParseInt(SimRequest.OptionalQuery(context, "limit"), "limit", errors, 1, 500) ?? DefaultLimit;

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        var matching = SimulatorData.Shipments
            .Where(s => s.UpdatedAt >= since!.Value && (type is null || s.ShipmentType == type))
            .ToList();

        var page = matching.Skip(offset).Take(limit).ToList();
        var next = offset + page.Count < matching.Count
            ? (offset + page.Count).ToString(CultureInfo.InvariantCulture)
            : null;

        return Results.Json(new ShipmentPage(page, next));
    }

    private static IResult GetByBlNumber(HttpContext context, string blNumber)
    {
        var errors = new RequestErrors();
        SimRequest.CheckLength(blNumber, "blNumber", errors, 4, 20);

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        var summary = SimulatorData.Shipments
            .FirstOrDefault(s => string.Equals(s.BlNumber, blNumber.Trim(), StringComparison.OrdinalIgnoreCase));

        return summary is null ? SimulatorHttp.NotFound(context) : Results.Json(ToDetail(summary));
    }

    private static Shipment ToDetail(ShipmentSummary s)
    {
        var import = s.ShipmentType == "IMPORT";
        var (origin, originName, originCountry) = import ? ("CNSHA", "Shanghai", "CN") : ("CLVAP", "Valparaíso", "CL");
        var (destination, destinationName, destinationCountry) = import ? ("CLSAI", "San Antonio", "CL") : ("DEHAM", "Hamburg", "DE");

        return new Shipment(
            s.BlNumber, s.BookingNumber, s.ShipmentType, s.Vessel, s.Voyage, s.Etd, s.Eta, s.FreeDays,
            s.DepotImport, s.DepotExport, s.MatchCode, s.TaxId, s.UpdatedAt,
            Route:
            [
                new RouteLeg("1", "SEA", 1, origin, originName, originCountry, s.Vessel, s.Voyage, null, s.Etd),
                new RouteLeg("2", "SEA", 2, destination, destinationName, destinationCountry, s.Vessel, s.Voyage, s.Eta, null),
            ],
            Equipments: [new Equipment("HLXU1234567", "45G1", 1, 1)],
            Parties: [new Party(import ? "CONSIGNEE" : "SHIPPER", s.MatchCode ?? "MC000000", null, s.TaxId)],
            Revenues: [new Revenue("HLXU1234567", "THC", "Terminal handling charge", 180m, "USD")],
            VesselCall: s.Vessel is null ? null : new VesselCall(s.Vessel, null, null, null),
            FreeTime: s.FreeDays is { } days && s.Eta is { } eta
                ? new FreeTime(days, DateOnly.FromDateTime(eta), DateOnly.FromDateTime(eta).AddDays(days - 1), [])
                : null);
    }
}
