namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations.Payments;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Pasarela simulada (modo de prueba): envía al simulador de pago del portal y su consulta de estado informa el
/// resultado elegido allí, con el monto y la moneda del pago del portal, sin depender de memoria propia.
/// </summary>
public sealed class DummyPaymentProviderTests
{
    private readonly InMemoryPaymentSimulatorStore _store = new();
    private readonly DummyPaymentProvider _provider;

    public DummyPaymentProviderTests()
    {
        _provider = NewProvider("Khipu");
    }

    private DummyPaymentProvider NewProvider(string code) => new(code, Substitute.For<ILogger<DummyPaymentProvider>>(), _store);

    private static PaymentInitiationRequest Request(string externalReference, string returnUrl = "/payments/abc/result") => new(
        externalReference, 15000m, "CLP", "Pago BL", returnUrl, "https://notify.url", "76000002-2");

    private static PaymentStatusRequest Status(string externalReference, string? providerReference = "DUMMY-KHIPU-x") =>
        new(providerReference, externalReference, 15000m, "CLP");

    [Fact]
    public void Properties_ShouldExposeProviderCodeAndNotVerifyNotifications()
    {
        _provider.ProviderCode.Should().Be("Khipu");
        _provider.VerifiesNotifications.Should().BeFalse();
        _provider.Should().BeAssignableTo<ISimulatedPaymentProvider>();
    }

    [Fact]
    public async Task InitiateAsync_ShouldReturnPendingWithDeterministicReference_AndTheSimulatorUrl()
    {
        var result = await _provider.InitiateAsync(Request("PAY-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("DUMMY-KHIPU-PAY-1");
        result.Value.ExternalReference.Should().Be("PAY-1");
        result.Value.Status.Should().Be(PaymentStatus.Pending);
        result.Value.Form.Should().BeNull("el simulador sirve también al botón de Banco de Chile, sin formulario firmado");
        result.Value.RedirectUrl.Should().Be(
            "/payments/simulator?provider=Khipu&ref=PAY-1&amount=15000&currency=CLP&returnUrl=%2Fpayments%2Fabc%2Fresult%3Fref%3DPAY-1");
    }

    [Fact]
    public async Task InitiateAsync_AbsoluteReturnUrl_ShouldUseTheSameOrigin()
    {
        var provider = NewProvider("BancoChile");

        var result = await provider.InitiateAsync(Request("PAY-9", "https://portal.example/payments/abc/result"));

        result.Value.RedirectUrl.Should().StartWith("https://portal.example/payments/simulator?provider=BancoChile&ref=PAY-9&amount=15000&currency=CLP&returnUrl=");
        var query = System.Web.HttpUtility.ParseQueryString(new Uri(result.Value.RedirectUrl).Query);
        query["returnUrl"].Should().Be("https://portal.example/payments/abc/result?ref=PAY-9");
    }

    [Theory]
    [InlineData(null, PaymentStatus.Processing)]
    [InlineData(PaymentSimulatorOutcomes.Pending, PaymentStatus.Processing)]
    [InlineData(PaymentSimulatorOutcomes.Approved, PaymentStatus.Confirmed)]
    [InlineData(PaymentSimulatorOutcomes.Rejected, PaymentStatus.Failed)]
    [InlineData(PaymentSimulatorOutcomes.Cancelled, PaymentStatus.Failed)]
    public async Task GetStatusAsync_ShouldReportTheSimulatorOutcome(string? outcome, string expected)
    {
        if (outcome is not null)
            _store.Record("PAY-2", outcome);

        var result = await _provider.GetStatusAsync(Status("PAY-2"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expected);
        result.Value.ExternalReference.Should().Be("PAY-2");
        result.Value.Amount.Should().Be(15000m);
        result.Value.Currency.Should().Be("CLP");
        if (expected == PaymentStatus.Confirmed)
            result.Value.TransactionId.Should().Be("DUMMY-TXN-PAY-2");
        else
            result.Value.TransactionId.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_AfterARestart_ShouldNotDependOnTheInitiatedPayment()
    {
        // Pago iniciado por una instancia anterior de la API: la nueva instancia no lo conoce, pero el almacén único
        // del simulador y los datos del pago del portal bastan.
        await _provider.InitiateAsync(Request("PAY-4"));
        _store.Record("PAY-4", PaymentSimulatorOutcomes.Approved);
        var afterRestart = NewProvider("Khipu");

        var result = await afterRestart.GetStatusAsync(Status("PAY-4"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Confirmed);
        result.Value.Amount.Should().Be(15000m);
    }

    [Fact]
    public async Task GetStatusAsync_NeverInitiatedWithoutOutcome_ShouldBeInProcess()
    {
        var result = await NewProvider("Bci").GetStatusAsync(Status("PAY-UNKNOWN"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public async Task GetStatusAsync_StoreSharedAcrossProviders()
    {
        _store.Record("PAY-5", PaymentSimulatorOutcomes.Approved);

        var result = await NewProvider("Santander").GetStatusAsync(Status("PAY-5"));

        result.Value.Status.Should().Be(PaymentStatus.Confirmed);
    }

    [Fact]
    public async Task GetStatusAsync_RejectReference_ShouldReturnFailed()
    {
        _store.Record("PAY-3", PaymentSimulatorOutcomes.Approved);

        var result = await _provider.GetStatusAsync(Status("PAY-3", "token-REJECT"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Failed);
        result.Value.TransactionId.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_WithoutAmount_ShouldReturnInvalidResponse()
    {
        var result = await _provider.GetStatusAsync(new PaymentStatusRequest("token-ok", "PAY-6"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Integration.InvalidResponse");
    }

    [Fact]
    public void Store_ShouldKeepTheLastOutcomeNormalized()
    {
        _store.Outcome("PAY-7").Should().BeNull();
        _store.Record("PAY-7", " Pending ");
        _store.Record("PAY-7", "APPROVED");

        _store.Outcome("PAY-7").Should().Be(PaymentSimulatorOutcomes.Approved);
    }

    [Fact]
    public async Task ReadNotificationAsync_ShouldRejectBecauseDummyUsesTheSimulatedWebhook()
    {
        var result = await _provider.ReadNotificationAsync(new PaymentNotification("{}", new Dictionary<string, string>()));

        result.IsFailure.Should().BeTrue();
        (await _provider.CancelAsync(new PaymentStatusRequest("x", "PAY-1"))).IsSuccess.Should().BeTrue();
    }
}
