using System.Collections.Concurrent;
using System.Globalization;

namespace HapagPortal.IntegrationSimulator;

public sealed record TaxDocument(
    string Folio,
    int DocumentType,
    string ExternalReference,
    string Status,
    string? SiiTrackId,
    DateOnly IssueDate,
    decimal TotalAmount,
    string Currency,
    string? PdfUrl,
    string? XmlUrl);

/// <summary>
/// CT-DBNET bajo <c>/dbnet</c>, con el comportamiento de <c>DummyInvoiceProvider</c>: documentos en
/// memoria con folios correlativos desde 100001, REJECTED si la referencia externa contiene "REJECT"
/// (si no, ACCEPTED) y la misma referencia devuelve el documento ya emitido.
/// </summary>
public static class DbNetEndpoints
{
    private static readonly int[] DocumentTypes = [33, 34, 39, 41, 61, 110];
    private static readonly ConcurrentDictionary<string, TaxDocument> ByFolio = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, TaxDocument> ByReference = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock Gate = new();
    private static int _lastFolio = 100000;

    public static void MapDbNet(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dbnet");
        group.MapPost("/documents", (Func<HttpContext, Task<IResult>>)IssueDocument);
        group.MapGet("/documents/{folio}", GetDocument);
    }

    private static async Task<IResult> IssueDocument(HttpContext context)
    {
        var errors = new RequestErrors();
        SimRequest.RequiredHeader(context, "Idempotency-Key", errors, maxLength: 64);
        var (document, body) = await JsonBody.ReadAsync(context, errors);
        using var _ = document;

        var documentType = body?.RequiredInteger("documentType");
        if (documentType is { } type && !DocumentTypes.Contains(type))
            errors.Add("documentType", "Tipo de DTE no admitido.");

        var externalReference = body?.RequiredString("externalReference");
        var issueDate = SimRequest.ParseDate(body?.RequiredString("issueDate"), "issueDate", errors);
        var currency = SimRequest.CheckCurrency(body?.RequiredString("currency"), "currency", errors);
        body?.OptionalNumber("exchangeRate", nullable: true);

        if (body?.RequiredObject("receiver") is { } receiver)
        {
            var taxId = receiver.RequiredString("taxId");
            if (taxId is not null && !SimRequest.RutPattern().IsMatch(taxId))
                errors.Add("receiver.taxId", "Debe ser un RUT con guion y dígito verificador.");
            receiver.RequiredString("businessName");
        }

        foreach (var line in body?.RequiredArrayOfObjects("lines", minItems: 1) ?? [])
        {
            line.RequiredInteger("lineNumber", minimum: 1);
            line.RequiredString("description");
            line.RequiredNumber("quantity", exclusiveMinimum: 0m);
            line.RequiredNumber("unitPrice", minimum: 0m);
            line.RequiredBoolean("exempt");
            line.RequiredNumber("amount", minimum: 0m);
        }

        decimal? totalAmount = null;
        if (body?.RequiredObject("totals") is { } totals)
        {
            totals.RequiredNumber("netAmount", minimum: 0m);
            totals.RequiredNumber("exemptAmount", minimum: 0m);
            totals.RequiredNumber("vatAmount", minimum: 0m);
            totalAmount = totals.RequiredNumber("totalAmount", minimum: 0m);
        }

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        TaxDocument issued;
        lock (Gate)
        {
            if (!ByReference.TryGetValue(externalReference!, out issued!))
            {
                var folio = (++_lastFolio).ToString(CultureInfo.InvariantCulture);
                var reject = externalReference!.Contains("REJECT", StringComparison.OrdinalIgnoreCase);

                issued = new TaxDocument(
                    folio, documentType!.Value, externalReference, reject ? "REJECTED" : "ACCEPTED", $"SIM-{folio}",
                    issueDate!.Value, totalAmount!.Value, currency!, null, null);

                ByFolio[folio] = issued;
                ByReference[externalReference] = issued;
            }
        }

        return Results.Json(issued);
    }

    private static IResult GetDocument(HttpContext context, string folio)
    {
        var errors = new RequestErrors();
        var documentType = SimRequest.ParseInt(SimRequest.OptionalQuery(context, "documentType"), "documentType", errors, int.MinValue, int.MaxValue);
        if (documentType is { } type && !DocumentTypes.Contains(type))
            errors.Add("documentType", "Tipo de DTE no admitido.");

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        return ByFolio.TryGetValue(folio, out var document)
            ? Results.Json(document)
            : SimulatorHttp.NotFound(context);
    }
}
