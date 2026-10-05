namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.Integrations.DbNet;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class DummyInvoiceProviderTests
{
    private readonly DummyInvoiceProvider _provider = new(Substitute.For<ILogger<DummyInvoiceProvider>>());

    private static InvoiceIssueRequest Request(string externalReference) => new(
        33,
        externalReference,
        new DateOnly(2026, 10, 20),
        "CLP",
        null,
        new InvoiceReceiver("76000002-2", "Cliente Crédito SpA", null, null, null, null, null),
        [new InvoiceLine(1, "KTE", "Cambio de almacén", 1, 9940m, false, 9940m)],
        new InvoiceTotals(9940m, 0m, 19m, 1889m, 11829m));

    [Fact]
    public async Task IssueAsync_ShouldReturnAcceptedDocumentWithFolio()
    {
        var result = await _provider.IssueAsync(Request("PAY-2026-000123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Folio.Should().Be("100001");
        result.Value.Status.Should().Be("ACCEPTED");
        result.Value.TotalAmount.Should().Be(11829m);
        result.Value.DocumentType.Should().Be(33);
    }

    [Fact]
    public async Task IssueAsync_RejectReference_ShouldReturnRejectedDocument()
    {
        var result = await _provider.IssueAsync(Request("PAY-REJECT-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("REJECTED");
    }

    [Fact]
    public async Task IssueAsync_SameReferenceTwice_ShouldReturnSameDocument()
    {
        var first = await _provider.IssueAsync(Request("PAY-2026-000124"));
        var second = await _provider.IssueAsync(Request("PAY-2026-000124"));

        second.Value.Should().Be(first.Value);
    }

    [Fact]
    public async Task GetAsync_IssuedFolio_ShouldReturnDocument()
    {
        var issued = await _provider.IssueAsync(Request("PAY-2026-000125"));

        var result = await _provider.GetAsync(issued.Value.Folio);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(issued.Value);
    }

    [Fact]
    public async Task GetAsync_UnknownFolio_ShouldReturnSuccessWithNull()
    {
        var result = await _provider.GetAsync("999999");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
