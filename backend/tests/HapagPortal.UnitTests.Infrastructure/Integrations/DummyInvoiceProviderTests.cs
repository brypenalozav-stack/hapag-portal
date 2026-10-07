namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Documents;
using HapagPortal.Infrastructure.Integrations.DbNet;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PdfSharp.Pdf.IO;

public sealed class DummyInvoiceProviderTests
{
    private readonly ServiceProvider _services;
    private readonly DummyInvoiceProvider _provider;

    public DummyInvoiceProviderTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        _services = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName))
            .AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())
            .BuildServiceProvider();
        _provider = new DummyInvoiceProvider(
            Substitute.For<ILogger<DummyInvoiceProvider>>(),
            _services.GetRequiredService<IServiceScopeFactory>(),
            new MigraDocPdfRenderer(),
            new DocumentSettings());
    }

    private static int PageCount(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return document.PageCount;
    }

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

    [Fact]
    public async Task GetPdfAsync_PortalInvoice_ShouldRenderReadablePdf()
    {
        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.CustomerInvoices.Add(new CustomerInvoice
            {
                OrganizationId = Guid.NewGuid(),
                SiiNumber = "100260",
                SourceNumber = "HL-CL-2026-003987",
                DocumentType = InvoiceDocumentTypes.Invoice,
                IssueDate = new DateOnly(2026, 9, 15),
                DueDate = new DateOnly(2026, 9, 30),
                BlNumber = "HLCUSAI260400910",
                LegalName = "Importadora Andes SpA",
                TaxId = "76123456-7",
                NetAmount = 500000m,
                TaxAmount = 95000m,
                TotalAmount = 595000m,
                Currency = "CLP",
                Status = InvoiceStatus.Overdue,
                Country = CountryCodes.Chile,
                Source = "SYNC"
            });
            await db.SaveChangesAsync();
        }

        var result = await _provider.GetPdfAsync("100260");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        if (Environment.GetEnvironmentVariable("HL_PDF_PREVIEW_DIR") is { Length: > 0 } folder)
            File.WriteAllBytes(Path.Combine(folder, "factura-simulada.pdf"), result.Value!);
        result.Value!.Length.Should().BeGreaterThan(1000, "un marcador de pocos bytes no abre en ningún visor");
        System.Text.Encoding.ASCII.GetString(result.Value, 0, 5).Should().Be("%PDF-");
        PageCount(result.Value).Should().Be(1);
    }

    [Fact]
    public async Task GetPdfAsync_FolioIssuedByDummy_ShouldRenderReadablePdf()
    {
        var issued = await _provider.IssueAsync(Request("PAY-2026-000130"));

        var result = await _provider.GetPdfAsync(issued.Value.Folio);

        result.IsSuccess.Should().BeTrue();
        PageCount(result.Value!).Should().Be(1);
    }

    [Fact]
    public async Task GetPdfAsync_UnknownFolio_ShouldReturnNull()
    {
        var result = await _provider.GetPdfAsync("999999");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
