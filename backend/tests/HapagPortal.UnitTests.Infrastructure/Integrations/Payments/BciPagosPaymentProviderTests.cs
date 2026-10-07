namespace HapagPortal.UnitTests.Infrastructure.Integrations.Payments;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations.Payments;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Bci Pagos (botón BCI): x_signature, creación, consulta con JWT en caché, mapeo tolerante y callback.</summary>
public sealed class BciPagosPaymentProviderTests
{
    private const string TokenSecret = "secret123";

    /// <summary>
    /// Firma esperada calculada fuera de .NET (Python: hmac.new(b"secret123", mensaje, sha256).hexdigest()) sobre el
    /// mensaje "x_account_idacc123x_amount53550x_currencyCLPx_customer_emaila@b.clx_referencePAY-1x_session_idPAY-1
    /// x_shop_countryCLx_url_callbackhttps://api.example/cbx_url_cancelhttps://portal.example/rx_url_complete
    /// https://portal.example/r" (sin saltos de línea).
    /// </summary>
    private const string ExpectedSignature = "0294feb50dfe6904a31b113e3190a6bc12a9e9366f49949706905d196cd8510b";

    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeGatewayHandler _handler = new();
    private readonly BciPagosTokenCache _cache = new();
    private readonly FixedTimeProvider _time = new(Now);

    private BciPagosPaymentProvider Provider(ISecretResolver? secrets = null, BciPagosOptions? options = null) =>
        new(_handler.Client(),
            secrets ?? GatewaySecrets.With(
                (SecretTypes.BciPagosAccountId, "acc123"), (SecretTypes.BciPagosTokenSecret, TokenSecret),
                (SecretTypes.BciPagosUsername, "usuario"), (SecretTypes.BciPagosPassword, "clave")),
            options ?? new BciPagosOptions(),
            _cache,
            new PaymentPublicUrls("https://portal.example", "https://api.example"),
            _time,
            NullLogger<BciPagosPaymentProvider>.Instance);

    private static PaymentInitiationRequest Request() =>
        new("PAY-1", 53550m, "CLP", "Pago", "/r", "/cb", "76000001-1", "a@b.cl");

    private const string Created = """
        {"status_code":201,"body":{"message":"Transacción creada","data":{"id_trx":"9001",
         "pay_url":[{"name":"webpay","url":"https://pgf.example/webpay/9001"},{"name":"gateway","url":"https://pgf.example/gateway/9001"}]}}}
        """;

    [Fact]
    public void Sign_ShouldMatchAnIndependentImplementation()
    {
        var fields = new Dictionary<string, string>
        {
            ["x_url_complete"] = "https://portal.example/r",
            ["x_amount"] = "53550",
            ["x_account_id"] = "acc123",
            ["x_currency"] = "CLP",
            ["x_customer_email"] = "a@b.cl",
            ["x_reference"] = "PAY-1",
            ["x_session_id"] = "PAY-1",
            ["x_shop_country"] = "CL",
            ["x_url_callback"] = "https://api.example/cb",
            ["x_url_cancel"] = "https://portal.example/r",
            ["x_signature"] = "se-ignora",
        };

        BciPagosSignature.Sign(fields, TokenSecret).Should().Be(ExpectedSignature);
    }

    [Fact]
    public void Message_ShouldSortKeysOrdinally()
    {
        BciPagosSignature.Message(new Dictionary<string, string> { ["x_b"] = "2", ["X_a"] = "1", ["x_a"] = "3" })
            .Should().Be("X_a1x_a3x_b2", "orden por bytes: las mayúsculas van antes");
    }

    [Fact]
    public async Task InitiateAsync_ShouldPostTheSignedTransactionWithoutAuthorizationHeader()
    {
        _handler.On("POST /trxs", HttpStatusCode.OK, Created);

        var result = await Provider().InitiateAsync(Request());

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("9001");
        result.Value.RedirectUrl.Should().Be("https://pgf.example/gateway/9001", "se prefiere la opción gateway");

        var sent = _handler.Requests.Single();
        sent.Headers.Should().NotContainKey("Authorization");
        var body = sent.Json;
        body.GetProperty("x_account_id").GetString().Should().Be("acc123");
        body.GetProperty("x_amount").GetRawText().Should().Be("53550");
        body.GetProperty("x_currency").GetString().Should().Be("CLP");
        body.GetProperty("x_reference").GetString().Should().Be("PAY-1");
        body.GetProperty("x_customer_email").GetString().Should().Be("a@b.cl");
        body.GetProperty("x_url_complete").GetString().Should().Be("https://portal.example/r");
        body.GetProperty("x_url_callback").GetString().Should().Be("https://api.example/cb");
        body.GetProperty("x_url_cancel").GetString().Should().Be("https://portal.example/r");
        body.GetProperty("x_shop_country").GetString().Should().Be("CL");
        body.GetProperty("x_session_id").GetString().Should().Be("PAY-1");
        body.GetProperty("x_signature").GetString().Should().Be(ExpectedSignature);
    }

    [Theory]
    [InlineData("""{"body":{"data":{"id_trx":1,"pay_url":["https://pgf.example/webpay","https://pgf.example/gateway"]}}}""", "https://pgf.example/gateway")]
    [InlineData("""{"body":{"data":{"id_trx":1,"pay_url":[{"link":"https://pgf.example/only"}]}}}""", "https://pgf.example/only")]
    [InlineData("""{"body":{"data":{"id_trx":1,"pay_url":"https://pgf.example/text"}}}""", "https://pgf.example/text")]
    [InlineData("""{"data":{"id_trx":1,"pay_url":[{"href":"https://pgf.example/a"},{"href":"https://pgf.example/b"}]}}""", "https://pgf.example/a")]
    public void PayUrl_ShouldBeParsedTolerantly(string json, string expected)
    {
        BciPagosPaymentProvider.PayUrl(JsonDocument.Parse(json).RootElement).Should().Be(expected);
    }

    [Fact]
    public async Task InitiateAsync_WithoutCredentialsOrEmail_ShouldFailNotConfigured()
    {
        (await Provider(GatewaySecrets.With()).InitiateAsync(Request())).Error.Code.Should().Be("Integration.NotConfigured");
        (await Provider().InitiateAsync(Request() with { PayerEmail = null })).Error.Code.Should().Be("Integration.NotConfigured");
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiateAsync_Errors_ShouldMapToIntegrationErrors()
    {
        _handler.On("POST /trxs", HttpStatusCode.InternalServerError, "{}");
        (await Provider().InitiateAsync(Request())).Error.Code.Should().Be("Integration.Unavailable");

        var bad = new FakeGatewayHandler();
        bad.On("POST /trxs", HttpStatusCode.Unauthorized, """{"message":"firma inválida"}""");
        var provider = new BciPagosPaymentProvider(bad.Client(),
            GatewaySecrets.With((SecretTypes.BciPagosAccountId, "a"), (SecretTypes.BciPagosTokenSecret, "s")), new BciPagosOptions(), _cache,
            new PaymentPublicUrls("https://portal.example", null), _time, NullLogger<BciPagosPaymentProvider>.Instance);
        (await provider.InitiateAsync(Request())).Error.Code.Should().Be("Integration.InvalidResponse");
    }

    private static string Jwt(DateTimeOffset expiresAt)
    {
        static string Part(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Part("{\"alg\":\"HS256\"}")}.{Part($"{{\"exp\":{expiresAt.ToUnixTimeSeconds()}}}")}.firma";
    }

    private static string Trx(string status, string? responseCode = null, string? authCode = null) =>
        "{\"message\":\"ok\",\"data\":{\"id_trx\":9001,\"order_id_tienda\":\"PAY-1\",\"amount\":\"53550\",\"status\":" + status +
        (responseCode is null ? "" : $",\"responce_code\":\"{responseCode}\"") +
        (authCode is null ? "" : $",\"auth_code\":\"{authCode}\"") + "}}";

    [Theory]
    [InlineData("\"completed\"", null, null, "Confirmed")]
    [InlineData("{\"name\":\"Pagada exitosamente\"}", null, null, "Confirmed")]
    [InlineData("\"pending\"", "0", "123456", "Confirmed")]
    [InlineData("\"Rechazada\"", null, null, "Failed")]
    [InlineData("{\"code\":\"CANCELLED\"}", null, null, "Failed")]
    [InlineData("\"pending\"", "0", null, "Pending")]
    [InlineData("\"en espera\"", null, null, "Pending")]
    public async Task GetStatusAsync_ShouldLoginQueryAndMapTolerantly(string status, string? responseCode, string? authCode, string expected)
    {
        _handler.On("POST /users/login", HttpStatusCode.OK, "{\"body\":{\"data\":{\"token\":\"" + Jwt(Now.AddHours(1)) + "\"}}}");
        _handler.On("GET /trxs/9001", HttpStatusCode.OK, Trx(status, responseCode, authCode));

        var result = await Provider().GetStatusAsync(new PaymentStatusRequest("9001", "PAY-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expected);
        result.Value.ExternalReference.Should().Be("PAY-1");
        result.Value.Amount.Should().Be(53550m);
        result.Value.Currency.Should().Be("CLP");
        var login = _handler.Requests.First();
        login.Json.GetProperty("username").GetString().Should().Be("usuario");
        _handler.Requests.Last().Headers["Authorization"].Should().StartWith("Bearer ");
    }

    [Fact]
    public async Task GetStatusAsync_ShouldCacheTheJwtUntilItExpires()
    {
        _handler.On("POST /users/login", HttpStatusCode.OK, "{\"token\":\"" + Jwt(Now.AddMinutes(30)) + "\"}");
        _handler.On("GET /trxs/9001", HttpStatusCode.OK, Trx("\"pending\""));

        await Provider().GetStatusAsync(new PaymentStatusRequest("9001", "PAY-1"));
        await Provider().GetStatusAsync(new PaymentStatusRequest("9001", "PAY-1"));
        _handler.Requests.Count(r => r.Path == "/users/login").Should().Be(1);

        _time.Now = Now.AddMinutes(31);
        await Provider().GetStatusAsync(new PaymentStatusRequest("9001", "PAY-1"));
        _handler.Requests.Count(r => r.Path == "/users/login").Should().Be(2);
    }

    [Fact]
    public async Task GetStatusAsync_RejectedToken_ShouldLoginAgainOnce()
    {
        _handler.On("POST /users/login", HttpStatusCode.OK, """{"access_token":"t"}""");
        _handler.On("GET /trxs/9001", HttpStatusCode.Unauthorized, "{}");
        _handler.On("GET /trxs/9001", HttpStatusCode.OK, Trx("\"completed\""));

        var result = await Provider().GetStatusAsync(new PaymentStatusRequest("9001", "PAY-1"));

        result.Value.Status.Should().Be(PaymentStatus.Confirmed);
        _handler.Requests.Count(r => r.Path == "/users/login").Should().Be(2);
    }

    [Fact]
    public async Task GetStatusAsync_ConfigurableStatusTexts_ShouldBeUsed()
    {
        _handler.On("POST /users/login", HttpStatusCode.OK, """{"token":"t"}""");
        _handler.On("GET /trxs/9001", HttpStatusCode.OK, Trx("\"LIQUIDADA\""));

        var result = await Provider(options: new BciPagosOptions { PaidStatusValues = ["liquidada"] })
            .GetStatusAsync(new PaymentStatusRequest("9001", "PAY-1"));

        result.Value.Status.Should().Be(PaymentStatus.Confirmed);
    }

    [Fact]
    public async Task GetStatusAsync_WithoutQueryCredentials_ShouldFailNotConfigured()
    {
        var result = await Provider(GatewaySecrets.With((SecretTypes.BciPagosAccountId, "a")))
            .GetStatusAsync(new PaymentStatusRequest("9001", "PAY-1"));

        result.Error.Code.Should().Be("Integration.NotConfigured");
    }

    [Fact]
    public async Task ReadNotificationAsync_WithValidSignature_ShouldReturnTheTransaction()
    {
        var fields = new Dictionary<string, string> { ["x_reference"] = "PAY-1", ["x_id_trx"] = "9001", ["x_result"] = "completed" };
        var form = string.Join("&", fields.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}")) +
            "&x_signature=" + BciPagosSignature.Sign(fields, TokenSecret);

        var result = await Provider().ReadNotificationAsync(
            new PaymentNotification(form, new Dictionary<string, string>(), "application/x-www-form-urlencoded"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("9001");
        result.Value.ExternalReference.Should().Be("PAY-1");
        result.Value.SignedStatus.Should().BeNull("el estado siempre se consulta");
    }

    [Fact]
    public async Task ReadNotificationAsync_WithInvalidSignature_ShouldBeUnauthorized()
    {
        const string json = """{"x_reference":"PAY-1","x_id_trx":"9001","x_signature":"00ff"}""";

        (await Provider().ReadNotificationAsync(new PaymentNotification(json, new Dictionary<string, string>(), "application/json")))
            .Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task ReadNotificationAsync_WithoutSignature_ShouldOnlyIdentifyThePayment()
    {
        const string json = """{"id_trx":9001}""";

        var result = await Provider().ReadNotificationAsync(new PaymentNotification(json, new Dictionary<string, string>(), "application/json"));

        result.Value.ProviderReference.Should().Be("9001");
        result.Value.ExternalReference.Should().BeNull();
    }
}
