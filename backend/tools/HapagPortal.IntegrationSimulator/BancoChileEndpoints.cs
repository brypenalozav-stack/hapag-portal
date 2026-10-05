using System.Collections.Concurrent;
using System.Globalization;

namespace HapagPortal.IntegrationSimulator;

public sealed record BankPaymentInitiation(string Reference, string ExternalReference, string Status, string RedirectUrl, DateTime ExpiresAt);

public sealed record BankPaymentStatus(
    string Reference,
    string ExternalReference,
    string Status,
    decimal Amount,
    string Currency,
    string? TransactionId,
    string? PayerTaxId,
    DateTime? PaidAt);

/// <summary>
/// CT-BCH bajo <c>/banco-chile</c>. Pagos en memoria, idempotentes por <c>Idempotency-Key</c>. La
/// consulta devuelve CONFIRMED, salvo que la referencia externa contenga "REJECT" (REJECTED), como en el
/// Dummy. Existe el pago de ejemplo del contrato: <c>BAN-7781230</c>.
/// </summary>
public static class BancoChileEndpoints
{
    private static readonly ConcurrentDictionary<string, BankPaymentStatus> PaymentsByReference = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, BankPaymentInitiation> InitiationsByKey = new(StringComparer.Ordinal);
    private static readonly Lock Gate = new();
    private static int _lastReference = 7800000;

    static BancoChileEndpoints()
    {
        PaymentsByReference["BAN-7781230"] = new BankPaymentStatus(
            "BAN-7781230", "PAY-2026-000123", "CONFIRMED", 245000m, "CLP", "000981234567", "76000001-1",
            new DateTime(2026, 10, 20, 14, 31, 55, DateTimeKind.Utc));
    }

    public static void MapBancoChile(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/banco-chile");
        group.MapPost("/payments", (Func<HttpContext, Task<IResult>>)InitiatePayment);
        group.MapGet("/payments/{reference}", GetPayment);
    }

    private static async Task<IResult> InitiatePayment(HttpContext context)
    {
        var errors = new RequestErrors();
        var idempotencyKey = SimRequest.RequiredHeader(context, "Idempotency-Key", errors, maxLength: 64);
        var (document, body) = await JsonBody.ReadAsync(context, errors);
        using var _ = document;

        var externalReference = body?.RequiredString("externalReference", maxLength: 50);
        var amount = body?.RequiredNumber("amount", exclusiveMinimum: 0m);
        var currency = SimRequest.CheckEnum(body?.RequiredString("currency"), "currency", errors, "CLP", "USD");
        body?.RequiredString("subject", maxLength: 140);
        var payerTaxId = body?.OptionalString("payerTaxId");
        body?.RequiredString("returnUrl");
        body?.RequiredString("notifyUrl");
        body?.OptionalString("cancelUrl");
        var expiresAt = SimRequest.ParseDateTime(body?.OptionalString("expiresAt"), "expiresAt", errors);

        if (payerTaxId is not null && !SimRequest.RutPattern().IsMatch(payerTaxId))
            errors.Add("payerTaxId", "Debe ser un RUT con guion y dígito verificador.");

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        BankPaymentInitiation initiation;
        lock (Gate)
        {
            if (!InitiationsByKey.TryGetValue(idempotencyKey!, out initiation!))
            {
                var reference = "BAN-" + (++_lastReference).ToString(CultureInfo.InvariantCulture);
                var reject = externalReference!.Contains("REJECT", StringComparison.OrdinalIgnoreCase);

                initiation = new BankPaymentInitiation(
                    reference, externalReference, "PENDING",
                    $"https://pagos.banco-chile.example/checkout/{reference}",
                    expiresAt ?? DateTime.UtcNow.Date.AddDays(1));

                InitiationsByKey[idempotencyKey!] = initiation;
                PaymentsByReference[reference] = new BankPaymentStatus(
                    reference, externalReference, reject ? "REJECTED" : "CONFIRMED", amount!.Value, currency!,
                    reject ? null : "000" + _lastReference.ToString("D9", CultureInfo.InvariantCulture),
                    payerTaxId,
                    reject ? null : DateTime.UtcNow);
            }
        }

        return Results.Json(initiation);
    }

    private static IResult GetPayment(HttpContext context, string reference) =>
        PaymentsByReference.TryGetValue(reference, out var payment)
            ? Results.Json(payment)
            : SimulatorHttp.NotFound(context);
}
