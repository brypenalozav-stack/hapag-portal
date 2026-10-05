namespace HapagPortal.IntegrationSimulator;

public sealed record Exemption(string Concept, decimal? Amount, string? Currency, string? Consignee, DateOnly ValidFrom, DateOnly? ValidTo);

public sealed record ExemptionList(string TaxId, string? MatchCode, DateOnly At, IReadOnlyList<Exemption> Items);

public sealed record CreditCondition(IReadOnlyList<string> Concepts, int CreditDays, DateOnly ValidFrom, DateOnly? ValidTo, string? ApprovedBy);

public sealed record CustomerConditions(string TaxId, string? MatchCode, bool IsFreightForwarder, CreditCondition? Credit);

public sealed record ExchangeRate(string FromCurrency, string ToCurrency, decimal Rate, DateOnly EffectiveDate, string Source, bool Approved);

public sealed record TariffItem(string Concept, string? Code, string? ContainerType, decimal Amount, string Currency, DateOnly ValidFrom, DateOnly? ValidTo);

public sealed record TariffList(string Country, string Concept, DateOnly At, IReadOnlyList<TariffItem> Items);

/// <summary>CT-NEXUS bajo <c>/nexus</c>.</summary>
public static class NexusEndpoints
{
    public static void MapNexus(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/nexus");
        group.MapGet("/exemptions", GetExemptions);
        group.MapGet("/customers/{taxId}/conditions", GetCustomerConditions);
        group.MapGet("/exchange-rates", GetExchangeRate);
        group.MapGet("/tariffs", GetTariffs);
    }

    private static IResult GetExemptions(HttpContext context)
    {
        var errors = new RequestErrors();
        var taxId = SimRequest.RequiredQuery(context, "taxId", errors, 3, 20);
        var matchCode = SimRequest.OptionalQuery(context, "matchCode");
        var at = SimRequest.ParseDate(SimRequest.OptionalQuery(context, "at"), "at", errors)
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        IReadOnlyList<Exemption> items = SimulatorData.IsTaxId(taxId, SimulatorData.ExemptTaxId) && at >= SimulatorData.ValidFrom
            ? [
                new Exemption("GATE_IN", null, null, null, SimulatorData.ValidFrom, null),
                new Exemption("EDS", null, null, null, SimulatorData.ValidFrom, null),
            ]
            : [];

        return Results.Json(new ExemptionList(taxId!, matchCode, at, items));
    }

    private static IResult GetCustomerConditions(HttpContext context, string taxId)
    {
        var errors = new RequestErrors();
        SimRequest.CheckLength(taxId, "taxId", errors, 3, 20);
        var matchCode = SimRequest.OptionalQuery(context, "matchCode");

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        CustomerConditions? conditions = null;

        if (SimulatorData.IsTaxId(taxId, SimulatorData.CreditTaxId))
        {
            conditions = new CustomerConditions(
                SimulatorData.CreditTaxId, matchCode, false,
                new CreditCondition(["LOCAL_CHARGES", "MHD"], 30, SimulatorData.ValidFrom, null, null));
        }
        else if (SimulatorData.IsTaxId(taxId, SimulatorData.FreightForwarderTaxId))
        {
            conditions = new CustomerConditions(SimulatorData.FreightForwarderTaxId, matchCode, true, null);
        }
        else if (SimulatorData.IsTaxId(taxId, SimulatorData.ExemptTaxId))
        {
            conditions = new CustomerConditions(SimulatorData.ExemptTaxId, matchCode, false, null);
        }

        return conditions is null ? SimulatorHttp.NotFound(context) : Results.Json(conditions);
    }

    private static IResult GetExchangeRate(HttpContext context)
    {
        var errors = new RequestErrors();
        var from = SimRequest.CheckCurrency(SimRequest.RequiredQuery(context, "from", errors), "from", errors);
        var to = SimRequest.CheckCurrency(SimRequest.RequiredQuery(context, "to", errors), "to", errors);
        var date = SimRequest.ParseDate(SimRequest.RequiredQuery(context, "date", errors), "date", errors);

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        if (!SimulatorData.UnitsPerUsd.TryGetValue(from!, out var fromUnits) ||
            !SimulatorData.UnitsPerUsd.TryGetValue(to!, out var toUnits))
        {
            return SimulatorHttp.NotFound(context);
        }

        return Results.Json(new ExchangeRate(from!, to!, Math.Round(toUnits / fromUnits, 6), date!.Value, SimulatorData.NexusSource, true));
    }

    private static IResult GetTariffs(HttpContext context)
    {
        var errors = new RequestErrors();
        var country = SimRequest.CheckEnum(SimRequest.RequiredQuery(context, "country", errors), "country", errors, "CL", "BO");
        var concept = SimRequest.RequiredQuery(context, "concept", errors);
        var at = SimRequest.ParseDate(SimRequest.OptionalQuery(context, "at"), "at", errors)
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        IReadOnlyList<TariffItem> items = country == "CL" && string.Equals(concept, "WAREHOUSE_CHANGE", StringComparison.OrdinalIgnoreCase) && at >= SimulatorData.TariffValidFrom
            ? [new TariffItem("WAREHOUSE_CHANGE", null, null, 9940m, "CLP", SimulatorData.TariffValidFrom, null)]
            : [];

        return Results.Json(new TariffList(country!, concept!, at, items));
    }
}
