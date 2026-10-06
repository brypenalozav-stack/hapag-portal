namespace HapagPortal.UnitTests.Application.AccountStatement;

using System.IO.Compression;
using System.Text;
using FluentAssertions;
using HapagPortal.Application.AccountStatement;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Payments.Settlements;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Estado de cuenta en línea (M7-03): saldos, vencido y por vencer, antigüedad por tramos configurables, segregación
/// por organización (como M7-01), filtros y orden, montos reales de los clientes con crédito y crédito disponible,
/// anticipo cruzado con la factura posterior y exportación a planilla.
/// </summary>
public sealed class AccountStatementTests
{
    private readonly FinanceFixture _f = new();
    private readonly DateOnly _today = BusinessCalendar.LocalDate(CountryCodes.Chile, DateTime.UtcNow);
    private readonly BillOfLading _bl;

    public AccountStatementTests()
    {
        _bl = _f.Rules.OwnBl("BL-STMT");
        _bl.FreightPaidAt = DateTime.UtcNow.AddDays(-30);
    }

    private Client Org => _f.Owner.Organization;

    private Task<Result<AccountStatementDto>> StatementAsync(GetAccountStatementQuery? query = null, PaymentsFixture.Actor? actor = null) =>
        _f.Statement(actor).Handle(query ?? new GetAccountStatementQuery(), CancellationToken.None);

    private void SeedAccount()
    {
        _f.Payments.NoConditions(Org);
        _f.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 100000m);                               // no facturado: 119.000
        _f.AddInvoice(Org, _bl, 100000m, 19000m, _today.AddDays(-10), sii: "1001");              // 1-30
        _f.AddInvoice(Org, _bl, 50000m, 9500m, _today.AddDays(-45), sii: "1002");                // 31-60
        _f.AddInvoice(Org, _bl, 20000m, 0m, _today.AddDays(-120), sii: "1003");                  // 91+
        _f.AddInvoice(Org, _bl, 10000m, 1900m, _today.AddDays(3), sii: "1004");                  // por vencer
        _f.AddInvoice(Org, _bl, 5000m, 0m, _today.AddDays(30), sii: "1005");                     // no vencida
        _f.Payments.AddInvoice(Org, _bl, 9999m, sii: "1006", status: InvoiceStatus.Cancelled);   // fuera del estado de cuenta
        _f.Payments.AddInvoice(Org, _bl, 3000m, sii: "1007", type: InvoiceDocumentTypes.CreditNote, status: InvoiceStatus.Paid);
    }

    [Fact]
    public async Task Statement_ShouldSummarizeBalancesAgingAndDueSoon()
    {
        SeedAccount();

        var result = await StatementAsync();

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : null);
        var statement = result.Value;
        var clp = statement.Summary.Single(s => s.Currency == "CLP");
        clp.InvoicedBalance.Should().Be(215400m);
        clp.Overdue.Should().Be(198500m);
        clp.DueSoon.Should().Be(11900m);
        clp.NotYetDue.Should().Be(16900m);
        clp.Uninvoiced.Should().Be(119000m);
        clp.TotalBalance.Should().Be(334400m);

        statement.Aging.Buckets.Select(b => b.Code).Should().Equal("CURRENT", "1-30", "31-60", "61-90", "91+");
        statement.Aging.Rows.Single().Amounts.Should().Equal(16900m, 119000m, 59500m, 0m, 20000m);
        statement.DueSoonDays.Should().Be(StatementSettingKeys.DefaultDueSoonDays);

        // Orden: vencidas (la más antigua primero), luego por vencimiento y al final lo no facturado.
        statement.Lines.Select(l => l.Number).Should().Equal("1003", "1002", "1001", "1004", "1005", ChargeConceptCodes.Thc);
        statement.Lines.Single(l => l.Number == "1004").DueSoon.Should().BeTrue();
        statement.Lines.Single(l => l.Number == "1001").AgingBucket.Should().Be("1-30");
        statement.Lines.Single(l => l.Number == "1003").DaysOverdue.Should().Be(120);
        statement.Lines.Should().NotContain(l => l.Number == "1006" || l.Number == "1007");
        statement.IsCreditCustomer.Should().BeFalse();
        statement.Credit.Should().BeNull();
        statement.Actions.PaymentChannel.Should().Be(AccountStatementBuilder.ChannelCart);
        statement.Lines.Should().OnlyContain(l => !l.CanImputeToCredit);
        statement.LastUpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Statement_ShouldUseTheConfiguredBucketsAndDueSoonDays()
    {
        _f.Payments.NoConditions(Org);
        _f.Db.ConfigurationSettingList.Add(new ConfigurationSetting { Scope = ConfigurationScopes.Global, Key = StatementSettingKeys.AgingBuckets, Value = "15,45" });
        _f.Db.ConfigurationSettingList.Add(new ConfigurationSetting { Scope = ConfigurationScopes.Global, Key = StatementSettingKeys.DueSoonDays, Value = "40" });
        _f.AddInvoice(Org, _bl, 1000m, 0m, _today.AddDays(-20), sii: "2001");
        _f.AddInvoice(Org, _bl, 2000m, 0m, _today.AddDays(35), sii: "2002");

        var statement = (await StatementAsync()).Value;

        statement.Aging.Buckets.Select(b => b.Code).Should().Equal("CURRENT", "1-15", "16-45", "46+");
        statement.Lines.Single(l => l.Number == "2001").AgingBucket.Should().Be("16-45");
        statement.Lines.Single(l => l.Number == "2002").DueSoon.Should().BeTrue();
        statement.DueSoonDays.Should().Be(40);
    }

    [Fact]
    public async Task Filters_ShouldApplyToLinesButNotToTheAccountSummary()
    {
        SeedAccount();
        var other = _f.Rules.OwnBl("BL-OTHER");
        other.FreightPaidAt = DateTime.UtcNow.AddDays(-30);
        _f.AddInvoice(Org, other, 7000m, 0m, _today.AddDays(5), sii: "3001");

        var overdue = (await StatementAsync(new GetAccountStatementQuery(Status: StatementStatuses.Overdue))).Value;
        var dueSoon = (await StatementAsync(new GetAccountStatementQuery(Status: StatementStatuses.DueSoon))).Value;
        var charges = (await StatementAsync(new GetAccountStatementQuery(DocumentType: StatementDocumentTypes.LocalCharge))).Value;
        var byBl = (await StatementAsync(new GetAccountStatementQuery(BlNumber: "bl-other"))).Value;
        var usd = (await StatementAsync(new GetAccountStatementQuery(Currency: "USD"))).Value;
        var byAmount = (await StatementAsync(new GetAccountStatementQuery(Sort: StatementSortFields.Amount, Direction: "desc"))).Value;

        overdue.Lines.Select(l => l.Number).Should().BeEquivalentTo("1001", "1002", "1003");
        dueSoon.Lines.Select(l => l.Number).Should().BeEquivalentTo("1004", "3001");
        charges.Lines.Should().ContainSingle(l => l.ConceptCode == ChargeConceptCodes.Thc && l.Kind == StatementLineKinds.Uninvoiced);
        byBl.Lines.Select(l => l.Number).Should().Equal("3001");
        usd.Lines.Should().BeEmpty();
        byAmount.Lines.First().Balance.Should().Be(119000m);
        overdue.Summary.Single().TotalBalance.Should().Be(341400m, "the summary always covers the whole account");
    }

    [Fact]
    public async Task Statement_ShouldBeSegregatedByOrganization()
    {
        SeedAccount();
        var grantor = _f.Payments.NewActor();
        var stranger = _f.Payments.NewActor();
        var grantedBl = AccessTestData.AddBl(_f.Db, grantor.Organization.Id, "BL-GRANTOR");
        AccessTestData.AddRole(_f.Db, grantedBl, grantor.Organization, ShipmentRoleCodes.Consignee);
        var hiddenBl = AccessTestData.AddBl(_f.Db, grantor.Organization.Id, "BL-HIDDEN");
        AccessTestData.AddRole(_f.Db, hiddenBl, grantor.Organization, ShipmentRoleCodes.Consignee);
        ThirdPartyTestData.AddGrant(_f.Db, grantor.Organization, Org, grantedBl,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.ViewInvoicesAsBilled]);
        _f.Payments.NoConditions(grantor.Organization);
        _f.AddInvoice(grantor.Organization, grantedBl, 4000m, 0m, _today.AddDays(-2), sii: "4001");
        _f.AddInvoice(grantor.Organization, hiddenBl, 8000m, 0m, _today.AddDays(-2), sii: "4002");
        _f.AddInvoice(stranger.Organization, null, 6000m, 0m, _today.AddDays(-2), sii: "4003");

        var own = (await StatementAsync()).Value;
        var granted = await StatementAsync(new GetAccountStatementQuery(OrganizationId: grantor.Organization.Id));
        var foreign = await StatementAsync(new GetAccountStatementQuery(OrganizationId: stranger.Organization.Id));

        own.Lines.Select(l => l.Number).Should().NotContain(["4001", "4002", "4003"]);
        granted.Value.Organization.Id.Should().Be(grantor.Organization.Id);
        granted.Value.Lines.Select(l => l.Number).Should().Equal("4001");
        foreign.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task CreditCustomer_ShouldSeeRealAmounts_AndTheAvailableCredit()
    {
        _f.Credit(limit: 1_000_000m);
        _f.AddCreditRule(ChargeConceptCodes.Thc);
        var thc = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);
        _f.Rules.AddCharge(_bl, ChargeConceptCodes.Ipo, 150m, "USD", taxRate: 0m);
        _f.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);
        _f.AddInvoice(Org, _bl, 100000m, 19000m, _today.AddDays(-5), sii: "5001");

        var statement = (await StatementAsync()).Value;

        statement.IsCreditCustomer.Should().BeTrue();
        var line = statement.Lines.Single(l => l.SourceId == thc.Id);
        line.TotalAmount.Should().Be(220150m);
        line.Balance.Should().Be(220150m, "credit customers see the real amount, never zero (M7-03)");
        line.CanImputeToCredit.Should().BeTrue();
        statement.Lines.Should().NotContain(l => l.ConceptCode == ChargeConceptCodes.Ipo, "IPO is excluded for credit customers (M4-03)");
        statement.Lines.Single(l => l.ConceptCode == ChargeConceptCodes.GateOut).CanImputeToCredit.Should().BeFalse("GATE_OUT has no rule");
        statement.Credit!.Limit.Should().Be(1_000_000m);
        statement.Credit.Used.Should().Be(119000m);
        statement.Credit.Available.Should().Be(881000m);
        statement.Actions.PaymentChannel.Should().Be(AccountStatementBuilder.ChannelAccount);
        statement.Actions.CanImputeToCredit.Should().BeTrue();
    }

    [Fact]
    public async Task CreditWithoutLimit_ShouldExplainWhyThereIsNoAvailableCredit()
    {
        _f.Credit(limit: null);

        var statement = (await StatementAsync()).Value;

        statement.Credit!.Available.Should().BeNull();
        statement.Credit.UnavailableReason.Should().Be(AccountStatementBuilder.ReasonLimitNotInformed);
    }

    [Fact]
    public async Task Advance_ShouldBeMatchedWithTheLaterInvoice_AndShowTheInvoiceAsCovered()
    {
        _f.Payments.NoConditions(Org);
        var gateOut = _f.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);
        var payment = await _f.PayByCartAsync(_f.Owner, _f.OwnTaxId, (PayableItemTypes.LocalCharge, gateOut.Id));

        var before = (await StatementAsync()).Value;
        var settlement = _f.Db.ChargeSettlementList.Single();
        settlement.Kind.Should().Be(SettlementKinds.Advance);
        settlement.Status.Should().Be(SettlementStatus.Open);
        before.Advances.Single().Status.Should().Be(SettlementStatus.Open);
        before.Lines.Should().NotContain(l => l.SourceId == gateOut.Id, "the charge was paid in advance");
        before.Summary.Single(s => s.Currency == "CLP").AdvancesUnapplied.Should().Be(71400m);

        // La factura llega después del pago: se cruza con el anticipo y queda cubierta (no vuelve a cobrarse).
        var invoice = _f.AddInvoice(Org, _bl, 60000m, 11400m, _today.AddDays(30), ChargeConceptCodes.GateOut, sii: "6001");
        var match = await new MatchSettlementsCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new MatchSettlementsCommand(), CancellationToken.None);
        var after = (await StatementAsync()).Value;

        match.Value.Matched.Should().Be(1);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.IsPayable.Should().BeFalse();
        invoice.PaymentId.Should().Be(payment.Id);
        var line = after.Lines.Single(l => l.SourceId == invoice.Id);
        line.Status.Should().Be(StatementStatuses.Covered);
        line.Balance.Should().Be(0m);
        line.CoveredBy!.PaymentNumber.Should().Be(payment.PaymentNumber);
        line.Payable.Should().BeFalse();
        after.Advances.Single().MatchedInvoiceNumber.Should().Be("6001");
        after.Summary.Single(s => s.Currency == "CLP").TotalBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Export_ShouldProduceAnXlsxWorkbookAndACsvWithBom()
    {
        SeedAccount();

        var xlsx = await _f.Export().Handle(new ExportAccountStatementQuery(), CancellationToken.None);
        var csv = await _f.Export().Handle(new ExportAccountStatementQuery(Format: StatementExportFormats.Csv, Language: "en"), CancellationToken.None);

        xlsx.Value.ContentType.Should().Be(SpreadsheetWriter.XlsxContentType);
        xlsx.Value.FileName.Should().EndWith(".xlsx");
        using (var zip = new ZipArchive(new MemoryStream(xlsx.Value.Content)))
        {
            zip.Entries.Select(e => e.FullName).Should().Contain(["xl/workbook.xml", "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml", "xl/worksheets/sheet3.xml"]);
            using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
            var sheet = reader.ReadToEnd();
            sheet.Should().Contain("Saldo").And.Contain("BL-STMT").And.Contain("<v>119000");
        }

        csv.Value.ContentType.Should().Be(SpreadsheetWriter.CsvContentType);
        csv.Value.Content.Take(3).Should().Equal(Encoding.UTF8.GetPreamble());
        var text = Encoding.UTF8.GetString(csv.Value.Content[3..]);
        text.Split("\r\n")[0].Should().StartWith("Kind,Document type,Number");
        text.Should().Contain("1003");
    }
}
