namespace HapagPortal.IntegrationTests.Integrations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.IntegrationSimulator;
using HapagPortal.IntegrationTests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Clientes Real de FIS, DBNet, tracking y Khipu (API v3) contra el simulador en memoria:
/// rutas relativas bajo el prefijo de <c>BaseUrl</c>, mapeo del contrato y 404 → sin datos.
/// </summary>
public sealed class RealClientsTests(WebApplicationFactory<Program> simulator)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly SimulatorTraffic _traffic = new();

    private ServiceProvider Build(string system) => SimulatorClientHost.Build(simulator, system, _traffic);

    [Fact]
    public async Task Fis_ByBlNumber_ShouldMapShipment()
    {
        using var provider = Build("Fis");

        var result = await provider.GetRequiredService<IShipmentSource>().GetByBlNumberAsync("HLCUSCL2609A1234");

        result.IsSuccess.Should().BeTrue();
        result.Value!.TaxId.Should().Be("76000001-1");
        result.Value.ShipmentType.Should().Be("IMPORT");
        result.Value.FreeDays.Should().Be(7);
        result.Value.Eta.Should().Be(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Fis_UnknownBl_ShouldReturnSuccessNull()
    {
        using var provider = Build("Fis");

        var result = await provider.GetRequiredService<IShipmentSource>().GetByBlNumberAsync("HLCUXXXX0000");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Fis_UpdatedSince_ShouldFilterByDateAndType()
    {
        using var provider = Build("Fis");
        var source = provider.GetRequiredService<IShipmentSource>();
        var since = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc);

        var all = await source.GetUpdatedSinceAsync(since, null);
        var exports = await source.GetUpdatedSinceAsync(since, "EXPORT");

        all.Value.Select(s => s.BlNumber).Should().Equal("HLCUSCL2609A5678", "HLCUVAP2610E0001");
        exports.Value.Should().ContainSingle().Which.MatchCode.Should().Be("MC000303");
    }

    [Fact]
    public async Task DbNet_IssueAndGet_ShouldRoundTrip()
    {
        using var provider = Build("DbNet");
        var invoices = provider.GetRequiredService<IInvoiceProvider>();
        var request = new InvoiceIssueRequest(
            33, $"PAY-IT-{Guid.NewGuid():N}"[..20], new DateOnly(2026, 10, 20), "CLP", 950m,
            new InvoiceReceiver("76000001-1", "Importadora Ejemplo SpA", null, null, null, null, null),
            [new InvoiceLine(1, "THC", "Terminal handling charge", 1m, 205882m, false, 205882m)],
            new InvoiceTotals(205882m, 0m, 19m, 39118m, 245000m));

        var issued = await invoices.IssueAsync(request);
        var again = await invoices.IssueAsync(request);
        var read = await invoices.GetAsync(issued.Value.Folio);

        issued.IsSuccess.Should().BeTrue();
        issued.Value.Status.Should().Be("ACCEPTED");
        issued.Value.TotalAmount.Should().Be(245000m);
        again.Value.Folio.Should().Be(issued.Value.Folio, "la misma referencia no emite otro documento");
        read.Value.Should().Be(issued.Value);
    }

    [Fact]
    public async Task DbNet_UnknownFolio_ShouldReturnSuccessNull()
    {
        using var provider = Build("DbNet");

        var result = await provider.GetRequiredService<IInvoiceProvider>().GetAsync("1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Tracking_HapagReference_ShouldReturnThreeEvents()
    {
        using var provider = Build("Tracking");

        var result = await provider.GetRequiredService<ITrackingProvider>().GetEventsAsync("HLCUSCL2609A1234");

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(e => e.EventCode).Should().Equal("LOAD", "DEPA", "ARRI");
        result.Value[2].LocationName.Should().Be("San Antonio");
        result.Value[2].UnLocode.Should().Be("CLSAI");
    }

    [Fact]
    public async Task Tracking_OtherReference_ShouldReturnEmptyList()
    {
        using var provider = Build("Tracking");

        var result = await provider.GetRequiredService<ITrackingProvider>().GetEventsAsync("ABCD1234");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Khipu_Initiate_ShouldReturnPaymentUrl()
    {
        using var provider = Build("Khipu");
        var khipu = provider.GetRequiredKeyedService<IPaymentProvider>("Khipu");

        var result = await khipu.InitiateAsync(new PaymentInitiationRequest(
            "PAY-IT-KHIPU-1", 1190m, "CLP", "Pago de prueba", "https://portal.example/retorno",
            "https://portal.example/api/v1/payments/webhook/khipu", "76000001-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalReference.Should().Be("PAY-IT-KHIPU-1");
        result.Value.Status.Should().Be(PaymentStatus.Pending);
        result.Value.RedirectUrl.Should().StartWith("https://");
    }

    [Fact]
    public async Task Khipu_GetContractExamplePayment_ShouldReturnConfirmedPayment()
    {
        using var provider = Build("Khipu");
        var khipu = provider.GetRequiredKeyedService<IPaymentProvider>("Khipu");

        var result = await khipu.GetStatusAsync(new PaymentStatusRequest(KhipuEndpoints.ExamplePaymentId, "PAY-2026-000123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalReference.Should().Be("PAY-2026-000123");
        result.Value.Status.Should().Be(PaymentStatus.Confirmed);
        result.Value.Amount.Should().Be(245000m);
        result.Value.Currency.Should().Be("CLP");
    }

    [Fact]
    public async Task Khipu_GetUnknownPayment_ShouldFail()
    {
        using var provider = Build("Khipu");
        var khipu = provider.GetRequiredKeyedService<IPaymentProvider>("Khipu");

        var result = await khipu.GetStatusAsync(new PaymentStatusRequest("desconocido", "PAY-X"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Integration.InvalidResponse");
    }

    [Fact]
    public async Task Khipu_CreateRejectedAndPendingPayments_ShouldMapStatusAndCancelOnlyPending()
    {
        using var provider = Build("Khipu");
        var khipu = provider.GetRequiredKeyedService<IPaymentProvider>("Khipu");

        var rejected = await khipu.InitiateAsync(new PaymentInitiationRequest(
            "PAY-IT-REJECT", 1190m, "CLP", "Pago", "https://portal.example/r", "https://portal.example/n", null));
        var pending = await khipu.InitiateAsync(new PaymentInitiationRequest(
            "PAY-IT-PENDING", 1190m, "CLP", "Pago", "https://portal.example/r", "https://portal.example/n", null));

        (await khipu.GetStatusAsync(new PaymentStatusRequest(rejected.Value.ProviderReference, "PAY-IT-REJECT")))
            .Value.Status.Should().Be(PaymentStatus.Failed);
        (await khipu.GetStatusAsync(new PaymentStatusRequest(pending.Value.ProviderReference, "PAY-IT-PENDING")))
            .Value.Status.Should().Be(PaymentStatus.Pending);
        (await khipu.CancelAsync(new PaymentStatusRequest(pending.Value.ProviderReference, "PAY-IT-PENDING"))).IsSuccess.Should().BeTrue();
        (await khipu.CancelAsync(new PaymentStatusRequest(KhipuEndpoints.ExamplePaymentId, "PAY-2026-000123"))).IsFailure.Should().BeTrue();
    }
}
