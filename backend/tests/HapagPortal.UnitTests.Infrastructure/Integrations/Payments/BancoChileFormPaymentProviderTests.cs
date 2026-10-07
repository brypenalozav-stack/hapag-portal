namespace HapagPortal.UnitTests.Infrastructure.Integrations.Payments;

using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations.Payments;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Botón de Banco de Chile: formulario firmado configurable, deshabilitado hasta tener el manual del banco.</summary>
public sealed class BancoChileFormPaymentProviderTests
{
    private const string Key = "llave-del-banco";
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 15, 0, 0, TimeSpan.Zero);

    private static BancoChileOptions Configured() => new()
    {
        FormUrl = "https://banco.example/pago",
        SignedFields = ["convenio", "orden", "monto", "fecha"],
        SignatureSeparator = "|",
        PaidStatusValues = ["APROBADO"],
        FailedStatusValues = ["RECHAZADO"],
        NotificationAcknowledgement = "ACEPTADO",
        ExtraFields = new Dictionary<string, string> { ["version"] = "1.0" },
    };

    private static BancoChileFormPaymentProvider Provider(BancoChileOptions options, ISecretResolver? secrets = null) =>
        new(secrets ?? GatewaySecrets.With((SecretTypes.BancoChileMerchantId, "CONV-1"), (SecretTypes.BancoChileSigningKey, Key)),
            options,
            new PaymentPublicUrls("https://portal.example", "https://api.example"),
            new FixedTimeProvider(Now),
            NullLogger<BancoChileFormPaymentProvider>.Instance);

    private static PaymentInitiationRequest Request(string currency = "CLP") =>
        new("PAY-3", 128940m, currency, "Pago", "/payments/abc/result", "/api/v1/payments/webhook/banco-chile", null);

    [Fact]
    public async Task InitiateAsync_WithoutTheBankManual_ShouldFailNotConfigured()
    {
        var result = await Provider(new BancoChileOptions()).InitiateAsync(Request());

        result.Error.Code.Should().Be("Integration.NotConfigured");
    }

    [Fact]
    public async Task InitiateAsync_WithoutSecrets_ShouldFailNotConfigured()
    {
        (await Provider(Configured(), GatewaySecrets.With()).InitiateAsync(Request())).Error.Code.Should().Be("Integration.NotConfigured");
    }

    [Fact]
    public async Task InitiateAsync_ShouldReturnASignedPostForm()
    {
        var result = await Provider(Configured()).InitiateAsync(Request());

        result.IsSuccess.Should().BeTrue();
        var form = result.Value.Form!;
        form.Method.Should().Be("POST");
        form.Action.Should().Be("https://banco.example/pago");
        form.Fields["version"].Should().Be("1.0");
        form.Fields["convenio"].Should().Be("CONV-1");
        form.Fields["orden"].Should().Be("PAY-3");
        form.Fields["monto"].Should().Be("128940");
        form.Fields["urlRetorno"].Should().Be("https://portal.example/payments/abc/result");
        form.Fields["urlNotificacion"].Should().Be("https://api.example/api/v1/payments/webhook/banco-chile");
        form.Fields["fecha"].Should().MatchRegex("^20261007\\d{6}$");

        var data = string.Join("|", form.Fields["convenio"], form.Fields["orden"], form.Fields["monto"], form.Fields["fecha"]);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Key));
        form.Fields["firma"].Should().Be(Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant());
    }

    [Fact]
    public async Task InitiateAsync_OtherCurrency_ShouldFail()
    {
        (await Provider(Configured()).InitiateAsync(Request("USD"))).IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("HMACSHA1", "Base64")]
    [InlineData("SHA256", "HexUpper")]
    public void SignedForm_ShouldSupportTheConfiguredAlgorithms(string algorithm, string encoding)
    {
        var fields = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        var data = Encoding.UTF8.GetBytes("12");
        var key = Encoding.UTF8.GetBytes("k");

#pragma warning disable CA5350
        var expected = algorithm == "HMACSHA1"
            ? Convert.ToBase64String(new HMACSHA1(key).ComputeHash(data))
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("12k")));
#pragma warning restore CA5350

        SignedForm.Sign(fields, ["a", "b"], "", "k", algorithm, encoding).Should().Be(expected);
        SignedForm.Sign(fields, ["a", "c"], "", "k", algorithm, encoding).Should().BeNull("falta un campo firmado");
        SignedForm.Sign(fields, ["a"], "", "k", "MD5", encoding).Should().BeNull();
    }

    private static string Notification(string status, string amount = "128940", string? signature = null)
    {
        var data = string.Join("|", "CONV-1", "PAY-3", amount, "20261007120000");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Key));
        var firma = signature ?? Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
        return $"convenio=CONV-1&orden=PAY-3&monto={amount}&fecha=20261007120000&estado={status}&idTransaccion=TX-9&firma={firma}";
    }

    [Theory]
    [InlineData("APROBADO", "Confirmed")]
    [InlineData("RECHAZADO", "Failed")]
    [InlineData("EN_PROCESO", "Pending")]
    public async Task ReadNotificationAsync_ValidSignature_ShouldReturnTheSignedStatusAndTheAck(string status, string expected)
    {
        var result = await Provider(Configured()).ReadNotificationAsync(new PaymentNotification(Notification(status), new Dictionary<string, string>()));

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalReference.Should().Be("PAY-3");
        result.Value.Acknowledgement.Should().Be("ACEPTADO");
        result.Value.SignedStatus!.Status.Should().Be(expected);
        result.Value.SignedStatus.Amount.Should().Be(128940m);
        result.Value.SignedStatus.Currency.Should().Be("CLP");
        result.Value.SignedStatus.TransactionId.Should().Be("TX-9");
    }

    [Fact]
    public async Task ReadNotificationAsync_InvalidSignature_ShouldBeUnauthorized()
    {
        var result = await Provider(Configured())
            .ReadNotificationAsync(new PaymentNotification(Notification("APROBADO", signature: "00"), new Dictionary<string, string>()));

        result.Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task ReadNotificationAsync_NotConfigured_ShouldBeUnauthorized()
    {
        (await Provider(new BancoChileOptions()).ReadNotificationAsync(new PaymentNotification(Notification("APROBADO"), new Dictionary<string, string>())))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatusAsync_WithoutAStatusQuery_ShouldBeNotConfigured()
    {
        (await Provider(Configured()).GetStatusAsync(new PaymentStatusRequest(null, "PAY-3"))).Error.Code.Should().Be("Integration.NotConfigured");
    }
}
