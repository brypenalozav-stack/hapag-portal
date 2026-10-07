namespace HapagPortal.UnitTests.Application.Invoices;

using System.IO.Compression;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Invoices;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Facturas del cliente (M7-01): segregación por organización (NF-05), filtros, estado vencido en la fecha
/// del país, descarga individual y múltiple solo con folio, y actualización desde la fuente.
/// </summary>
public sealed class InvoiceTests
{
    private readonly PaymentsFixture _f = new();
    private readonly IInvoiceProvider _provider = Substitute.For<IInvoiceProvider>();
    private readonly BillOfLading _bl;

    public InvoiceTests()
    {
        _bl = _f.Rules.OwnBl("BL-INV");
        _provider.GetPdfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<byte[]?>.Success(System.Text.Encoding.ASCII.GetBytes($"%PDF {call.Arg<string>()}"))));
    }

    private Task<Result<InvoiceListDto>> ListAsync(PaymentsFixture.Actor actor, GetInvoicesQuery query) =>
        new GetInvoicesQueryHandler(_f.Db, actor.CurrentUser, actor.Evaluator(_f.Db)).Handle(query, CancellationToken.None);

    [Fact]
    public async Task Invoices_ShouldBeSegregatedByOrganization()
    {
        var own = _f.AddInvoice(_f.Owner.Organization, _bl, 100000m);
        var other = _f.NewActor();
        var foreign = _f.AddInvoice(other.Organization, null, 50000m);

        var mine = await ListAsync(_f.Owner, new GetInvoicesQuery());
        var asking = await ListAsync(_f.Owner, new GetInvoicesQuery(OrganizationId: other.Organization.Id));
        var pdf = await new GetInvoicePdfQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _provider)
            .Handle(new GetInvoicePdfQuery(foreign.Id), CancellationToken.None);

        mine.Value.Organization.Id.Should().Be(_f.Owner.Organization.Id);
        mine.Value.Items.Select(i => i.Id).Should().Equal(own.Id);
        mine.Value.LastUpdatedAt.Should().Be(own.SyncedAt);
        asking.Error.Should().Be(Error.Forbidden);
        pdf.Error.Code.Should().Be("Invoice.NotFound");
    }

    [Fact]
    public async Task Grantee_ShouldSeeOnlyTheGrantorInvoicesOfBlsWhoseAccessIncludesInvoices()
    {
        var withInvoices = _bl;
        var withoutInvoices = _f.Rules.OwnBl("BL-NOINV");
        var visible = _f.AddInvoice(_f.Owner.Organization, withInvoices, 100000m);
        _f.AddInvoice(_f.Owner.Organization, withoutInvoices, 70000m);
        var grantee = _f.NewActor();
        ThirdPartyTestData.AddGrant(_f.Db, _f.Owner.Organization, grantee.Organization, withInvoices,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.ViewInvoicesAsBilled]);
        ThirdPartyTestData.AddGrant(_f.Db, _f.Owner.Organization, grantee.Organization, withoutInvoices,
            [ShipmentActionCodes.ViewShipment]);

        var organizations = await new GetInvoiceOrganizationsQueryHandler(_f.Db, grantee.Evaluator(_f.Db))
            .Handle(new GetInvoiceOrganizationsQuery(), CancellationToken.None);
        var list = await ListAsync(grantee, new GetInvoicesQuery(OrganizationId: _f.Owner.Organization.Id));

        organizations.Value.Select(o => (o.Id, o.IsOwn)).Should().Equal(
            (grantee.Organization.Id, true), (_f.Owner.Organization.Id, false));
        list.Value.Items.Select(i => i.Id).Should().Equal(visible.Id);
    }

    [Fact]
    public async Task Filters_ShouldApplyToBlBookingDatesStatusCurrencyAndType()
    {
        var other = _f.Rules.OwnBl("BL-OTHER");
        var overdue = _f.AddInvoice(_f.Owner.Organization, _bl, 100000m, due: new DateOnly(2026, 1, 15));
        var usd = _f.AddInvoice(_f.Owner.Organization, other, 350m, currency: "USD");
        var paid = _f.AddInvoice(_f.Owner.Organization, other, 80000m, status: InvoiceStatus.Paid);
        var note = _f.AddInvoice(_f.Owner.Organization, other, 5000m, type: InvoiceDocumentTypes.CreditNote);
        usd.IssueDate = new DateOnly(2026, 9, 20);

        (await ListAsync(_f.Owner, new GetInvoicesQuery(BlNumber: "bl-inv"))).Value.Items.Select(i => i.Id).Should().Equal(overdue.Id);
        (await ListAsync(_f.Owner, new GetInvoicesQuery(Status: InvoiceStatus.Overdue))).Value.Items.Single().Status.Should().Be(InvoiceStatus.Overdue);
        (await ListAsync(_f.Owner, new GetInvoicesQuery(Status: InvoiceStatus.Paid))).Value.Items.Select(i => i.Id).Should().Equal(paid.Id);
        (await ListAsync(_f.Owner, new GetInvoicesQuery(Currency: "usd"))).Value.Items.Select(i => i.Id).Should().Equal(usd.Id);
        (await ListAsync(_f.Owner, new GetInvoicesQuery(DocumentType: InvoiceDocumentTypes.CreditNote))).Value.Items.Select(i => i.Id).Should().Equal(note.Id);
        (await ListAsync(_f.Owner, new GetInvoicesQuery(From: new DateOnly(2026, 9, 15), To: new DateOnly(2026, 9, 30)))).Value.Items
            .Select(i => i.Id).Should().Equal(usd.Id);

        var all = await ListAsync(_f.Owner, new GetInvoicesQuery(PageSize: 2));
        all.Value.Total.Should().Be(4);
        all.Value.Items.Should().HaveCount(2);
        all.Value.TimeZone.Should().Be("America/Santiago");
        (await ListAsync(_f.Owner, new GetInvoicesQuery())).Value.Items.Single(i => i.Id == note.Id).IsPayable.Should().BeFalse();
    }

    [Fact]
    public async Task Download_ShouldRequireAFolio_AndZipSeveral()
    {
        var first = _f.AddInvoice(_f.Owner.Organization, _bl, 100000m, sii: "100245");
        var second = _f.AddInvoice(_f.Owner.Organization, _bl, 50000m, sii: "100246");
        var noFolio = _f.AddInvoice(_f.Owner.Organization, _bl, 350m, sii: null);
        var evaluator = _f.Owner.Evaluator(_f.Db);

        var single = await new GetInvoicePdfQueryHandler(_f.Db, evaluator, _provider).Handle(new GetInvoicePdfQuery(first.Id), CancellationToken.None);
        var withoutFolio = await new GetInvoicePdfQueryHandler(_f.Db, evaluator, _provider).Handle(new GetInvoicePdfQuery(noFolio.Id), CancellationToken.None);
        var mixed = await new DownloadInvoicesQueryHandler(_f.Db, evaluator, _provider)
            .Handle(new DownloadInvoicesQuery([first.Id, noFolio.Id]), CancellationToken.None);
        var zip = await new DownloadInvoicesQueryHandler(_f.Db, evaluator, _provider)
            .Handle(new DownloadInvoicesQuery([first.Id, second.Id]), CancellationToken.None);

        single.Value.FileName.Should().Be("factura-100245.pdf");
        single.Value.ContentType.Should().Be("application/pdf");
        withoutFolio.Error.Code.Should().Be("Invoice.PdfNotAvailable");
        mixed.Error.Code.Should().Be("Invoice.FolioRequired");
        zip.Value.ContentType.Should().Be("application/zip");
        using var archive = new ZipArchive(new MemoryStream(zip.Value.Content));
        archive.Entries.Select(e => e.Name).Should().Equal("factura-100245.pdf", "factura-100246.pdf");
    }

    [Fact]
    public async Task Refresh_ShouldUpdateSiiStatusAndTimestamp_OrNothingIfTheSourceFails()
    {
        var invoice = _f.AddInvoice(_f.Owner.Organization, _bl, 100000m, sii: "100245");
        var before = invoice.SyncedAt;
        _provider.GetAsync("100245", Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<InvoiceDocument?>.Success(
            new InvoiceDocument("100245", 33, "X", "ACCEPTED_WITH_OBJECTIONS", null, new DateOnly(2026, 9, 1), 100000m, "CLP", null, null))));

        var refreshed = await new RefreshInvoicesCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _provider)
            .Handle(new RefreshInvoicesCommand(null), CancellationToken.None);

        refreshed.Value.Updated.Should().Be(1);
        invoice.SiiStatus.Should().Be("ACCEPTED_WITH_OBJECTIONS");
        invoice.SyncedAt.Should().BeAfter(before);

        var stamp = invoice.SyncedAt;
        _provider.GetAsync("100245", Arg.Any<CancellationToken>()).Returns(Task.FromResult(
            Result<InvoiceDocument?>.Failure(new Error("Integration.Unavailable", "down"))));
        var failed = await new RefreshInvoicesCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _provider)
            .Handle(new RefreshInvoicesCommand(null), CancellationToken.None);

        failed.Error.Code.Should().Be("Integration.Unavailable");
        invoice.SyncedAt.Should().Be(stamp);
    }
}
