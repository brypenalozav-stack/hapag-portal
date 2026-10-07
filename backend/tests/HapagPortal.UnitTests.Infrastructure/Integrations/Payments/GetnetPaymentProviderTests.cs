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

/// <summary>Getnet Web Checkout (botón Santander): auth/tranKey, sesión, consulta, anulación y firma de la notificación.</summary>
public sealed class GetnetPaymentProviderTests
{
    private const string Login = "login-de-prueba";
    private const string SecretKey = "024h1IlD";
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeGatewayHandler _handler = new();

    private GetnetPaymentProvider Provider(ISecretResolver? secrets = null) =>
        new(_handler.Client(),
            secrets ?? GatewaySecrets.With((SecretTypes.GetnetLogin, Login), (SecretTypes.GetnetSecretKey, SecretKey)),
            new GetnetOptions(),
            new PaymentPublicUrls("https://portal.example", null),
            new FixedTimeProvider(Now),
            NullLogger<GetnetPaymentProvider>.Instance);

    private static PaymentInitiationRequest Request() => new(
        "PAY-20261007-00000002", 128940m, "CLP", "Hapag-Lloyd PAY-20261007-00000002", "/payments/abc/result",
        "/api/v1/payments/webhook/getnet", "76000001-1", "cliente@example.com", "Importadora Andes SpA", "200.1.2.3", "Mozilla/5.0");

    /// <summary>tranKey con una implementación independiente: SHA-256 incremental sobre nonce, seed y secretKey.</summary>
    private static string IndependentTranKey(byte[] nonce, string seed, string secretKey)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(nonce);
        sha.AppendData(Encoding.UTF8.GetBytes(seed));
        sha.AppendData(Encoding.UTF8.GetBytes(secretKey));
        return Convert.ToBase64String(sha.GetHashAndReset());
    }

    private static void AssertAuth(JsonElement auth)
    {
        auth.GetProperty("login").GetString().Should().Be(Login);
        var seed = auth.GetProperty("seed").GetString()!;
        seed.Should().Be("2026-10-07T15:00:00+00:00");
        var nonce = Convert.FromBase64String(auth.GetProperty("nonce").GetString()!);
        nonce.Should().HaveCount(16);
        auth.GetProperty("tranKey").GetString().Should().Be(IndependentTranKey(nonce, seed, SecretKey));
    }

    [Fact]
    public void TranKey_ShouldHashTheRawNonceBytes_NotItsBase64()
    {
        var nonce = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
        var auth = GetnetAuth.Create(Login, SecretKey, new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.FromHours(-3)), nonce);

        auth["seed"].Should().Be("2026-10-07T12:00:00-03:00");
        auth["nonce"].Should().Be(Convert.ToBase64String(nonce));
        auth["tranKey"].Should().Be(IndependentTranKey(nonce, "2026-10-07T12:00:00-03:00", SecretKey));
        auth["tranKey"].Should().NotBe(IndependentTranKey(Encoding.UTF8.GetBytes(auth["nonce"]), auth["seed"], SecretKey));
    }

    [Fact]
    public async Task InitiateAsync_ShouldCreateTheSessionWithAuthAndRedirectToProcessUrl()
    {
        _handler.On("POST /api/session", HttpStatusCode.OK, """
            {"status":{"status":"OK","reason":"PC","message":"La petición se ha procesado correctamente","date":"2026-10-07T12:00:00-03:00"},
             "requestId":5976,"processUrl":"https://checkout.test.getnet.cl/spa/session/5976/abc"}
            """);

        var result = await Provider().InitiateAsync(Request());

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("5976");
        result.Value.RedirectUrl.Should().Be("https://checkout.test.getnet.cl/spa/session/5976/abc");

        var body = _handler.Requests.Single().Json;
        AssertAuth(body.GetProperty("auth"));
        body.GetProperty("locale").GetString().Should().Be("es_CL");
        body.GetProperty("payment").GetProperty("reference").GetString().Should().Be("PAY-20261007-00000002");
        body.GetProperty("payment").GetProperty("amount").GetProperty("currency").GetString().Should().Be("CLP");
        body.GetProperty("payment").GetProperty("amount").GetProperty("total").GetRawText().Should().Be("128940");
        body.GetProperty("expiration").GetString().Should().Be("2026-10-07T15:30:00+00:00");
        body.GetProperty("returnUrl").GetString().Should().Be("https://portal.example/payments/abc/result");
        body.GetProperty("ipAddress").GetString().Should().Be("200.1.2.3");
        body.GetProperty("userAgent").GetString().Should().Be("Mozilla/5.0");
        body.GetProperty("buyer").GetProperty("email").GetString().Should().Be("cliente@example.com");
        _handler.Requests.Single().Headers.Should().NotContainKey("Authorization");
    }

    [Fact]
    public async Task InitiateAsync_WithoutClientIp_ShouldUseTheFallbackIpAndUserAgent()
    {
        _handler.On("POST /api/session", HttpStatusCode.OK, """{"status":{"status":"OK"},"requestId":"1","processUrl":"https://x"}""");

        await Provider().InitiateAsync(Request() with { ClientIpAddress = null, ClientUserAgent = null });

        var body = _handler.Requests.Single().Json;
        body.GetProperty("ipAddress").GetString().Should().Be("127.0.0.1");
        body.GetProperty("userAgent").GetString().Should().Be("HapagPortal/1.0");
    }

    [Fact]
    public async Task InitiateAsync_RejectedAuthentication_ShouldBeInvalidResponse()
    {
        _handler.On("POST /api/session", HttpStatusCode.Unauthorized,
            """{"status":{"status":"FAILED","reason":401,"message":"Autenticación fallida 101"}}""");

        (await Provider().InitiateAsync(Request())).Error.Code.Should().Be("Integration.InvalidResponse");
    }

    [Fact]
    public async Task InitiateAsync_WithoutCredentials_ShouldFailNotConfigured()
    {
        var result = await Provider(GatewaySecrets.With((SecretTypes.GetnetLogin, Login))).InitiateAsync(Request());

        result.Error.Code.Should().Be("Integration.NotConfigured");
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiateAsync_ServerErrorAndTimeout_ShouldMapToIntegrationErrors()
    {
        _handler.On("POST /api/session", HttpStatusCode.InternalServerError, "{}");
        (await Provider().InitiateAsync(Request())).Error.Code.Should().Be("Integration.Unavailable");

        var timeout = new FakeGatewayHandler();
        timeout.OnThrow("POST /api/session", new TaskCanceledException());
        var provider = new GetnetPaymentProvider(timeout.Client(),
            GatewaySecrets.With((SecretTypes.GetnetLogin, Login), (SecretTypes.GetnetSecretKey, SecretKey)), new GetnetOptions(),
            new PaymentPublicUrls("https://portal.example", null), new FixedTimeProvider(Now), NullLogger<GetnetPaymentProvider>.Instance);
        (await provider.InitiateAsync(Request())).Error.Code.Should().Be("Integration.Timeout");
    }

    private static string Session(string status, string paymentStatus = "APPROVED", string total = "128940") =>
        "{\"requestId\":5976,\"status\":{\"status\":\"" + status + "\",\"reason\":\"00\",\"message\":\"x\",\"date\":\"2026-10-07T12:05:00-03:00\"}," +
        "\"request\":{\"payment\":{\"reference\":\"PAY-20261007-00000002\",\"amount\":{\"currency\":\"CLP\",\"total\":128940}}}," +
        "\"payment\":[{\"status\":{\"status\":\"" + paymentStatus + "\"},\"internalReference\":1447466623,\"reference\":\"PAY-20261007-00000002\"," +
        "\"authorization\":\"999999\",\"receipt\":\"1234\",\"amount\":{\"from\":{\"currency\":\"CLP\",\"total\":" + total + "}," +
        "\"to\":{\"currency\":\"CLP\",\"total\":" + total + "}}}]}";

    [Theory]
    [InlineData("APPROVED", "Confirmed")]
    [InlineData("REJECTED", "Failed")]
    [InlineData("PENDING", "Pending")]
    [InlineData("APPROVED_PARTIAL", "Pending")]
    [InlineData("PARTIAL_EXPIRED", "Failed")]
    public async Task GetStatusAsync_ShouldQueryTheSessionAndMapTheStatus(string status, string expected)
    {
        _handler.On("POST /api/session/5976", HttpStatusCode.OK, Session(status, status == "APPROVED" ? "APPROVED" : "REJECTED"));

        var result = await Provider().GetStatusAsync(new PaymentStatusRequest("5976", "PAY-20261007-00000002", 128940m, "CLP"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expected);
        result.Value.ExternalReference.Should().Be("PAY-20261007-00000002");
        result.Value.Amount.Should().Be(128940m);
        AssertAuth(_handler.Requests.Single().Json.GetProperty("auth"));
    }

    [Fact]
    public async Task GetStatusAsync_Approved_ShouldTakeTheAmountAndAuthorizationOfTheApprovedAttempt()
    {
        _handler.On("POST /api/session/5976", HttpStatusCode.OK, Session("APPROVED", total: "1000"));

        var result = await Provider().GetStatusAsync(new PaymentStatusRequest("5976", "PAY-20261007-00000002"));

        result.Value.Amount.Should().Be(1000m, "el monto cobrado es el del intento aprobado");
        result.Value.TransactionId.Should().Be("999999");
    }

    [Fact]
    public async Task CancelAsync_ShouldPostToTheCancelPath()
    {
        _handler.On("POST /api/session/5976/cancel", HttpStatusCode.OK, """{"status":{"status":"OK"}}""");

        (await Provider().CancelAsync(new PaymentStatusRequest("5976", "PAY-1"))).IsSuccess.Should().BeTrue();
        AssertAuth(_handler.Requests.Single().Json.GetProperty("auth"));
    }

    // --- Notificación: sha256:hex(SHA-256(requestId + status + date + secretKey)) o hex(SHA-1(...)) ---

    private static string NotificationBody(string signature) =>
        "{\"status\":{\"status\":\"APPROVED\",\"reason\":\"00\",\"message\":\"Aprobada\",\"date\":\"2026-10-07T12:05:00-03:00\"}," +
        "\"requestId\":5976,\"reference\":\"PAY-20261007-00000002\",\"signature\":\"" + signature + "\"}";

    private static string Data => "5976" + "APPROVED" + "2026-10-07T12:05:00-03:00" + SecretKey;

    [Fact]
    public async Task ReadNotificationAsync_Sha256Signature_ShouldBeAccepted()
    {
        var signature = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Data)));

        var result = await Provider().ReadNotificationAsync(new PaymentNotification(NotificationBody(signature), new Dictionary<string, string>()));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("5976");
        result.Value.ExternalReference.Should().Be("PAY-20261007-00000002");
    }

    [Fact]
    public async Task ReadNotificationAsync_LegacySha1Signature_ShouldBeAccepted()
    {
#pragma warning disable CA5350
        var signature = Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(Data)));
#pragma warning restore CA5350

        (await Provider().ReadNotificationAsync(new PaymentNotification(NotificationBody(signature), new Dictionary<string, string>())))
            .IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("sha256:0000")]
    [InlineData("")]
    [InlineData("deadbeef")]
    public async Task ReadNotificationAsync_InvalidSignature_ShouldBeUnauthorized(string signature)
    {
        (await Provider().ReadNotificationAsync(new PaymentNotification(NotificationBody(signature), new Dictionary<string, string>())))
            .Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task ReadNotificationAsync_SignatureOfAnotherStatus_ShouldBeUnauthorized()
    {
        var forged = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Data.Replace("APPROVED", "REJECTED"))));

        (await Provider().ReadNotificationAsync(new PaymentNotification(NotificationBody(forged), new Dictionary<string, string>())))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void VerifyNotificationSignature_ShouldUseTheDateTextAsReceived()
    {
        var signature = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("1APPROVED2026-10-07T12:05:00-03:00k")));

        GetnetAuth.VerifyNotificationSignature(signature, "1", "APPROVED", "2026-10-07T12:05:00-03:00", "k").Should().BeTrue();
        GetnetAuth.VerifyNotificationSignature(signature, "1", "APPROVED", "2026-10-07T15:05:00+00:00", "k").Should().BeFalse();
    }
}
