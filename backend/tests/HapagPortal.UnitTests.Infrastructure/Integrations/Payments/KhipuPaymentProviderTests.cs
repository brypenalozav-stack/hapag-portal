namespace HapagPortal.UnitTests.Infrastructure.Integrations.Payments;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations.Payments;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Khipu API v3: forma de las solicitudes, mapeo de estados, errores y firma de la notificación v3.</summary>
public sealed class KhipuPaymentProviderTests
{
    private const string ApiKey = "api-key-de-prueba";
    private const string WebhookSecret = "secreto-de-la-cuenta-de-cobro";
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeGatewayHandler _handler = new();
    private readonly FixedTimeProvider _time = new(Now);

    private HttpKhipuPaymentProvider Provider(
        ISecretResolver? secrets = null,
        PaymentPublicUrls? urls = null,
        KhipuOptions? options = null) =>
        new(_handler.Client(),
            secrets ?? GatewaySecrets.With((SecretTypes.KhipuSecret, ApiKey), (SecretTypes.KhipuWebhookSecret, WebhookSecret)),
            options ?? new KhipuOptions(),
            urls ?? new PaymentPublicUrls("https://portal.example", "https://api.portal.example"),
            _time,
            NullLogger<HttpKhipuPaymentProvider>.Instance);

    private static PaymentInitiationRequest Request(decimal amount = 245000m) => new(
        "PAY-20261007-00000001", amount, "CLP", "Hapag-Lloyd PAY-20261007-00000001", "/payments/abc/result",
        "/api/v1/payments/webhook/khipu", "76000001-1", "cliente@example.com", "Importadora Andes SpA");

    private const string PaymentJson = """
        {"payment_id":"gqzdy6chjne9","payment_url":"https://khipu.com/payment/info/gqzdy6chjne9",
         "simplified_transfer_url":"https://app.khipu.com/payment/simplified/gqzdy6chjne9",
         "transfer_url":"https://khipu.com/payment/manual/gqzdy6chjne9","app_url":"khipu:///pos/gqzdy6chjne9","ready_for_terminal":false}
        """;

    private static string Status(string status, string detail, string amount = "245000") =>
        "{\"payment_id\":\"gqzdy6chjne9\",\"subject\":\"Pago\",\"amount\":" + amount + ",\"currency\":\"CLP\",\"status\":\"" + status +
        "\",\"status_detail\":\"" + detail + "\",\"transaction_id\":\"PAY-20261007-00000001\",\"receiver_id\":985101}";

    [Fact]
    public async Task InitiateAsync_ShouldPostV3PaymentWithApiKeyAndAbsoluteUrls()
    {
        _handler.On("POST /v3/payments", HttpStatusCode.OK, PaymentJson);

        var result = await Provider().InitiateAsync(Request());

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("gqzdy6chjne9");
        result.Value.RedirectUrl.Should().Be("https://khipu.com/payment/info/gqzdy6chjne9");
        result.Value.Status.Should().Be(PaymentStatus.Pending);
        result.Value.Form.Should().BeNull();

        var sent = _handler.Requests.Single();
        sent.Headers["x-api-key"].Should().Be(ApiKey);
        var body = sent.Json;
        body.GetProperty("amount").ValueKind.Should().Be(JsonValueKind.Number);
        body.GetProperty("amount").GetRawText().Should().Be("245000", "CLP va como entero");
        body.GetProperty("currency").GetString().Should().Be("CLP");
        body.GetProperty("subject").GetString().Should().Be("Hapag-Lloyd PAY-20261007-00000001");
        body.GetProperty("transaction_id").GetString().Should().Be("PAY-20261007-00000001");
        body.GetProperty("return_url").GetString().Should().Be("https://portal.example/payments/abc/result");
        body.GetProperty("cancel_url").GetString().Should().Be("https://portal.example/payments/abc/result");
        body.GetProperty("notify_url").GetString().Should().Be("https://api.portal.example/api/v1/payments/webhook/khipu");
        body.GetProperty("notify_api_version").GetString().Should().Be("3.0");
        body.GetProperty("expires_date").GetString().Should().Be("2026-10-07T13:00:00.000Z");
        body.GetProperty("payer_email").GetString().Should().Be("cliente@example.com");
        body.TryGetProperty("fixed_payer_personal_identifier", out _).Should().BeFalse("solo con FixPayerTaxId");
        body.TryGetProperty("notification_token", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InitiateAsync_WithFixPayerTaxId_ShouldSendTheRut()
    {
        _handler.On("POST /v3/payments", HttpStatusCode.OK, PaymentJson);

        await Provider(options: new KhipuOptions { FixPayerTaxId = true, ExpirationMinutes = 0 }).InitiateAsync(Request(1190.4m));

        var body = _handler.Requests.Single().Json;
        body.GetProperty("fixed_payer_personal_identifier").GetString().Should().Be("76000001-1");
        body.GetProperty("amount").GetRawText().Should().Be("1190");
        body.TryGetProperty("expires_date", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InitiateAsync_WithoutApiKey_ShouldFailNotConfigured_WithoutCallingKhipu()
    {
        var result = await Provider(GatewaySecrets.With()).InitiateAsync(Request());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Integration.NotConfigured");
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiateAsync_WithoutPublicBaseUrl_ShouldFailNotConfigured()
    {
        var result = await Provider(urls: new PaymentPublicUrls(null, null)).InitiateAsync(Request());

        result.Error.Code.Should().Be("Integration.NotConfigured");
        result.Error.Message.Should().Contain("Payments:PublicBaseUrl");
        _handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Integration.InvalidResponse")]
    [InlineData(HttpStatusCode.BadRequest, "Integration.InvalidResponse")]
    [InlineData(HttpStatusCode.InternalServerError, "Integration.Unavailable")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Integration.Unavailable")]
    public async Task InitiateAsync_HttpErrors_ShouldMapToIntegrationErrors(HttpStatusCode status, string code)
    {
        _handler.On("POST /v3/payments", status, """{"status":401,"message":"Invalid api key"}""");

        var result = await Provider().InitiateAsync(Request());

        result.Error.Code.Should().Be(code);
    }

    [Fact]
    public async Task InitiateAsync_Timeout_ShouldMapToIntegrationTimeout()
    {
        _handler.OnThrow("POST /v3/payments", new TaskCanceledException("timeout"));

        (await Provider().InitiateAsync(Request())).Error.Code.Should().Be("Integration.Timeout");
    }

    [Fact]
    public async Task InitiateAsync_NetworkError_ShouldMapToIntegrationUnavailable()
    {
        _handler.OnThrow("POST /v3/payments", new HttpRequestException("refused"));

        (await Provider().InitiateAsync(Request())).Error.Code.Should().Be("Integration.Unavailable");
    }

    [Fact]
    public async Task InitiateAsync_ResponseWithoutPaymentUrl_ShouldBeInvalidResponse()
    {
        _handler.On("POST /v3/payments", HttpStatusCode.OK, """{"payment_id":"x"}""");

        (await Provider().InitiateAsync(Request())).Error.Code.Should().Be("Integration.InvalidResponse");
    }

    [Theory]
    [InlineData("done", "normal", "Confirmed")]
    [InlineData("done", "marked-paid-by-receiver", "Confirmed")]
    [InlineData("done", "reversed", "Failed")]
    [InlineData("done", "rejected-by-payer", "Failed")]
    [InlineData("pending", "marked-as-abuse", "Failed")]
    [InlineData("verifying", "normal", "Processing")]
    [InlineData("pending", "pending", "Pending")]
    public async Task GetStatusAsync_ShouldQueryByPaymentIdAndMapTheStatus(string status, string detail, string expected)
    {
        _handler.On("GET /v3/payments/gqzdy6chjne9", HttpStatusCode.OK, Status(status, detail));

        var result = await Provider().GetStatusAsync(new PaymentStatusRequest("gqzdy6chjne9", "PAY-20261007-00000001"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expected);
        result.Value.ExternalReference.Should().Be("PAY-20261007-00000001");
        result.Value.Amount.Should().Be(245000m);
        result.Value.Currency.Should().Be("CLP");
        _handler.Requests.Single().Headers["x-api-key"].Should().Be(ApiKey);
    }

    [Fact]
    public async Task GetStatusAsync_AmountAsText_ShouldBeParsed()
    {
        _handler.On("GET /v3/payments/gqzdy6chjne9", HttpStatusCode.OK, Status("done", "normal", "\"245000.0000\""));

        var result = await Provider().GetStatusAsync(new PaymentStatusRequest("gqzdy6chjne9", "PAY-20261007-00000001"));

        result.Value.Amount.Should().Be(245000m);
    }

    [Fact]
    public async Task GetStatusAsync_UnknownPayment_ShouldBeInvalidResponse()
    {
        _handler.On("GET /v3/payments/nope", HttpStatusCode.NotFound, """{"status":404,"message":"not found"}""");

        var result = await Provider().GetStatusAsync(new PaymentStatusRequest("nope", "PAY-1"));

        result.Error.Code.Should().Be("Integration.InvalidResponse");
    }

    [Fact]
    public async Task CancelAsync_ShouldDeleteThePayment()
    {
        _handler.On("DELETE /v3/payments/gqzdy6chjne9", HttpStatusCode.OK, """{"message":"Payment deleted"}""");

        var result = await Provider().CancelAsync(new PaymentStatusRequest("gqzdy6chjne9", "PAY-1"));

        result.IsSuccess.Should().BeTrue();
        _handler.Requests.Single().Method.Should().Be(HttpMethod.Delete);
    }

    // --- Notificación v3: x-khipu-signature: t=<ms>,s=<base64(HMAC-SHA256(secret, t + "." + body))> ---

    private const string NotificationBody =
        "{\"payment_id\":\"gqzdy6chjne9\",\"receiver_id\":985101,\"subject\":\"Pago\",\"amount\":\"245000.0000\",\"currency\":\"CLP\"," +
        "\"transaction_id\":\"PAY-20261007-00000001\",\"payer_email\":\"cliente@example.com\"}";

    /// <summary>Firma calculada con una implementación independiente (instancia de HMACSHA256).</summary>
    private static string IndependentSignature(string timestamp, string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
    }

    private static PaymentNotification Notification(string? header, string body = NotificationBody) =>
        new(body, header is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["X-Khipu-Signature"] = header });

    [Fact]
    public async Task ReadNotificationAsync_ValidSignature_ShouldReturnThePayment()
    {
        var t = Now.ToUnixTimeMilliseconds().ToString();
        var header = $"t={t},s={IndependentSignature(t, NotificationBody, WebhookSecret)}";

        var result = await Provider().ReadNotificationAsync(Notification(header));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("gqzdy6chjne9");
        result.Value.ExternalReference.Should().Be("PAY-20261007-00000001");
        result.Value.SignedStatus.Should().BeNull("el estado siempre se consulta");
        KhipuSignature.Compute(t, NotificationBody, WebhookSecret).Should().Be(IndependentSignature(t, NotificationBody, WebhookSecret));
    }

    [Fact]
    public async Task ReadNotificationAsync_TamperedBody_ShouldBeUnauthorized()
    {
        var t = Now.ToUnixTimeMilliseconds().ToString();
        var header = $"t={t},s={IndependentSignature(t, NotificationBody, WebhookSecret)}";

        var result = await Provider().ReadNotificationAsync(Notification(header, NotificationBody.Replace("245000", "1")));

        result.Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task ReadNotificationAsync_ReformattedJson_ShouldBeUnauthorized_BecauseTheRawBodyIsSigned()
    {
        var t = Now.ToUnixTimeMilliseconds().ToString();
        var header = $"t={t},s={IndependentSignature(t, NotificationBody, WebhookSecret)}";
        var reformatted = JsonSerializer.Serialize(JsonDocument.Parse(NotificationBody).RootElement, new JsonSerializerOptions { WriteIndented = true });

        (await Provider().ReadNotificationAsync(Notification(header, reformatted))).IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(-6)]
    [InlineData(6)]
    public async Task ReadNotificationAsync_OutsideTheTolerance_ShouldBeUnauthorized(int minutes)
    {
        var t = Now.AddMinutes(minutes).ToUnixTimeMilliseconds().ToString();
        var header = $"t={t},s={IndependentSignature(t, NotificationBody, WebhookSecret)}";

        (await Provider().ReadNotificationAsync(Notification(header))).IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("s=abc")]
    [InlineData("t=abc,s=abc")]
    [InlineData("t=1759838400000,s=@@no-base64@@")]
    public async Task ReadNotificationAsync_MissingOrMalformedHeader_ShouldBeUnauthorized(string? header)
    {
        (await Provider().ReadNotificationAsync(Notification(header))).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ReadNotificationAsync_OtherSecret_ShouldBeUnauthorized()
    {
        var t = Now.ToUnixTimeMilliseconds().ToString();
        var header = $"t={t},s={IndependentSignature(t, NotificationBody, "otro-secreto")}";

        (await Provider().ReadNotificationAsync(Notification(header))).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ReadNotificationAsync_WithoutWebhookSecret_ShouldFailClosed()
    {
        var t = Now.ToUnixTimeMilliseconds().ToString();
        var header = $"t={t},s={IndependentSignature(t, NotificationBody, string.Empty)}";

        var provider = Provider(GatewaySecrets.With((SecretTypes.KhipuSecret, ApiKey)));

        (await provider.ReadNotificationAsync(Notification(header))).Error.Code.Should().Be("Error.Unauthorized");
    }
}
