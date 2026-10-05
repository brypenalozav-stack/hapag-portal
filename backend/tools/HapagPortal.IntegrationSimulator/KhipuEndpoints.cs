using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace HapagPortal.IntegrationSimulator;

public sealed record KhipuPaymentCreated(
    string PaymentId,
    string PaymentUrl,
    string SimplifiedTransferUrl,
    string TransferUrl,
    string AppUrl,
    bool ReadyForTerminal);

public sealed record KhipuPayment(
    string PaymentId,
    string PaymentUrl,
    string NotificationToken,
    long ReceiverId,
    string Subject,
    decimal Amount,
    string Currency,
    string Status,
    string StatusDetail,
    string TransactionId);

/// <summary>
/// CT-KHIPU bajo <c>/khipu</c>, con los nombres de campo snake_case de la API pública v3. Pagos en
/// memoria. El token de notificación de un pago creado es <c>ntf-&lt;payment_id&gt;</c>; además existe el
/// pago de ejemplo del contrato (token <see cref="ExampleNotificationToken"/>, transaction_id
/// <c>PAY-2026-000123</c>, 245.000 CLP, pagado). Como en el Dummy, un <c>transaction_id</c> que contiene
/// "REJECT" queda rechazado por el pagador.
/// </summary>
public static class KhipuEndpoints
{
    public const string ExampleNotificationToken = "j8kPBHaPNy3PkCh2iqTYjXxGZnzW6vBzbZJzqDCe5";
    public const long ReceiverId = 985101;

    private static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly ConcurrentDictionary<string, KhipuPayment> PaymentsById = new(StringComparer.Ordinal);
    private static int _lastId;

    static KhipuEndpoints()
    {
        Store(new KhipuPayment(
            "gqzdy6chjne9", "https://khipu.com/payment/info/gqzdy6chjne9", ExampleNotificationToken, ReceiverId,
            "Pago PAY-2026-000123 – BL HLCUSCL2609A1234", 245000m, "CLP", "done", "normal", "PAY-2026-000123"));
    }

    public static void MapKhipu(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/khipu");
        group.MapPost("/v3/payments", (Func<HttpContext, Task<IResult>>)CreatePayment);
        group.MapGet("/v3/payments", GetByNotificationToken);
        group.MapGet("/v3/payments/{id}", GetById);
    }

    private static async Task<IResult> CreatePayment(HttpContext context)
    {
        var errors = new RequestErrors();
        var (document, body) = await JsonBody.ReadAsync(context, errors);
        using var _ = document;

        var amount = body?.RequiredNumber("amount", exclusiveMinimum: 0m);
        var currency = body?.RequiredString("currency");
        var subject = body?.RequiredString("subject", maxLength: 255);
        var transactionId = body?.OptionalString("transaction_id", maxLength: 255);

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        var id = "sim" + Interlocked.Increment(ref _lastId).ToString("x8", CultureInfo.InvariantCulture);
        var reject = transactionId?.Contains("REJECT", StringComparison.OrdinalIgnoreCase) == true;
        var payment = Store(new KhipuPayment(
            id, $"https://khipu.com/payment/info/{id}", $"ntf-{id}", ReceiverId, subject!, amount!.Value, currency!,
            reject ? "pending" : "done", reject ? "rejected-by-payer" : "normal", transactionId ?? id));

        return Results.Json(
            new KhipuPaymentCreated(
                payment.PaymentId,
                payment.PaymentUrl,
                $"https://app.khipu.com/payment/simplified/{id}",
                $"https://khipu.com/payment/manual/{id}",
                $"khipu:///pos/{id}",
                false),
            SnakeCase);
    }

    private static IResult GetByNotificationToken(HttpContext context)
    {
        var errors = new RequestErrors();
        var token = SimRequest.RequiredQuery(context, "notification_token", errors, minLength: 1);

        if (errors.Any)
            return SimulatorHttp.BadRequest(context, errors);

        var payment = PaymentsById.Values.FirstOrDefault(p => p.NotificationToken == token);
        return payment is null ? SimulatorHttp.NotFound(context) : Results.Json(payment, SnakeCase);
    }

    private static IResult GetById(HttpContext context, string id) =>
        PaymentsById.TryGetValue(id, out var payment)
            ? Results.Json(payment, SnakeCase)
            : SimulatorHttp.NotFound(context);

    private static KhipuPayment Store(KhipuPayment payment) => PaymentsById[payment.PaymentId] = payment;
}
