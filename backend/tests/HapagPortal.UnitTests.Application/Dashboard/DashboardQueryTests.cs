namespace HapagPortal.UnitTests.Application.Dashboard;

using FluentAssertions;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Dashboard;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Dashboard del cliente (M1-05): consolida gestiones, pendientes de pago, documentos e indicadores respetando
/// los accesos (M1-11, NF-05): lo de otra organización, lo que la matriz no habilita y los BL no publicados no
/// aparecen.
/// </summary>
public sealed class DashboardQueryTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly Client _org;
    private readonly IShipmentAccessEvaluator _evaluator;
    private readonly IChargeRulesService _rules = Substitute.For<IChargeRulesService>();

    public DashboardQueryTests()
    {
        var client = AccessTestData.ClientContext(_db);
        _org = client.Organization;
        _evaluator = client.Evaluator;
        _rules.GetConditionsAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>())
            .Returns(new CommercialConditionsDto(true, "NEXUS", _org.TaxId, null, false, null, [], null, null, false, false, false, null));
    }

    private GetDashboardQueryHandler Handler(IShipmentAccessEvaluator? evaluator = null) => new(_db, evaluator ?? _evaluator, _rules);

    private BillOfLading Bl(string number, string role, string type = "Import", Guid? owner = null)
    {
        var bl = AccessTestData.AddBl(_db, owner ?? Guid.NewGuid(), number, type, status: "Arrived");
        bl.ETA = DateTime.UtcNow.AddDays(-5);
        AccessTestData.AddRole(_db, bl, _org, role);
        return bl;
    }

    private static LocalCharge Charge(BillOfLading bl, string concept, decimal total, string status = ChargeStatus.Pending)
    {
        var charge = new LocalCharge
        {
            ChargeType = concept, Currency = "CLP", Status = status, TotalAmount = total, Amount = total, BillOfLadingId = bl.Id
        };
        bl.LocalCharges.Add(charge);
        return charge;
    }

    [Fact]
    public async Task PendingPayments_ShouldIncludeOnlyWhatTheUserMayPay()
    {
        var consignee = Bl("BL-CONS", ShipmentRoleCodes.Consignee);
        Charge(consignee, ChargeConceptCodes.Thc, 220150m);
        Charge(consignee, ChargeConceptCodes.Eds, 45220m, ChargeStatus.Paid);
        consignee.DemurrageCharges.Add(new DemurrageCharge
        {
            ContainerNumber = "HLXU1", Currency = "CLP", Status = DemurrageChargeStatus.Pending, TotalAmount = 510000m, BillOfLadingId = consignee.Id
        });

        // Shipper: el flete es X (o) y el demurrage de importación X.
        var shipper = Bl("BL-SHIP", ShipmentRoleCodes.Shipper, type: "Export");
        shipper.DemurrageCharges.Add(new DemurrageCharge
        {
            ContainerNumber = "HLXU2", Currency = "CLP", Status = DemurrageChargeStatus.Pending, TotalAmount = 99m, BillOfLadingId = shipper.Id
        });

        var foreign = AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-FOREIGN");
        Charge(foreign, ChargeConceptCodes.Thc, 1m);

        var result = await Handler().Handle(new GetDashboardQuery(), CancellationToken.None);

        var items = result.Value.PendingPayments.Items;
        items.Should().Contain(i => i.BlNumber == "BL-CONS" && i.ItemType == PayableItemTypes.LocalCharge && i.TotalAmount == 220150m);
        items.Should().Contain(i => i.BlNumber == "BL-CONS" && i.ItemType == PayableItemTypes.Demurrage && i.Target.Kind == DashboardTargets.Demurrage);
        items.Should().Contain(i => i.BlNumber == "BL-CONS" && i.ItemType == PayableItemTypes.Freight);
        items.Should().NotContain(i => i.BlNumber == "BL-FOREIGN");
        items.Should().NotContain(i => i.BlNumber == "BL-SHIP" && (i.ItemType == PayableItemTypes.Freight || i.ItemType == PayableItemTypes.Demurrage));
        result.Value.PendingPayments.Totals.Should().ContainSingle(t => t.Currency == "CLP" && t.Total == 220150m + 510000m);
    }

    [Fact]
    public async Task Indicators_ShouldCountAccessibleShipmentsAndFlagDemurrageAtRisk()
    {
        var consignee = Bl("BL-CONS", ShipmentRoleCodes.Consignee);
        Charge(consignee, ChargeConceptCodes.Thc, 10m);
        Bl("BL-EXP", ShipmentRoleCodes.Shipper, type: "Export");
        var hidden = Bl("BL-ANF", ShipmentRoleCodes.Consignee);
        hidden.FinalDestinationCode = "CLANF";
        hidden.PortOfDischargeCode = "CLSAI";
        _db.ShipmentPublicationRuleList.Add(new ShipmentPublicationRule { Country = CountryCodes.Chile, FinalDestinationCode = "CLANF", CreatedBy = "t" });
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-FOREIGN");

        var result = await Handler().Handle(new GetDashboardQuery(), CancellationToken.None);

        var indicators = result.Value.Indicators;
        indicators.TotalShipments.Should().Be(2);
        indicators.ByOperation.Should().BeEquivalentTo([new DashboardCountDto("EXPORT", 1), new DashboardCountDto("IMPORT", 1)]);
        indicators.WithPendingCharges.Should().Be(1);
        indicators.DemurrageAtRisk.Items.Should().ContainSingle(d => d.BlNumber == "BL-CONS" && d.State == DemurrageStates.NotCalculated);
    }

    [Fact]
    public async Task Documents_ShouldListOnlyTheTypesTheUserMaySee()
    {
        var bl = Bl("BL-CONS", ShipmentRoleCodes.Consignee);
        ShipmentDocument Document(string type, string number) => new()
        {
            DocumentType = type, DocumentNumber = number, Status = ShipmentDocumentStatus.Issued, BillOfLadingId = bl.Id,
            BlNumber = bl.BLNumber, Country = bl.Country, IssuedAt = DateTime.UtcNow, Origin = ShipmentDocumentOrigins.Seed,
            FileName = number + ".pdf", ContentType = "application/pdf", VerificationCode = "X", TemplateJson = "{}"
        };
        _db.ShipmentDocumentList.Add(Document(ShipmentDocumentTypes.BlCopyNonValued, "CBL-1"));
        _db.ShipmentDocumentList.Add(Document(ShipmentDocumentTypes.CollectReceipt, "CCO-1"));    // solo agencias (M6-04)

        var result = await Handler().Handle(new GetDashboardQuery(), CancellationToken.None);

        result.Value.Documents.Should().ContainSingle(d => d.DocumentNumber == "CBL-1")
            .Which.DownloadPath.Should().Be($"/api/v1/documents/BL-CONS/{_db.ShipmentDocumentList[0].Id}/download");
    }

    [Fact]
    public async Task Requests_ShouldShowTheOrganizationRequestsInProgress()
    {
        var bl = Bl("BL-CONS", ShipmentRoleCodes.Consignee);
        _db.WarehouseChangeList.Add(new WarehouseChange
        {
            FromWarehouse = "STI", ToWarehouse = "Bodega", Currency = "CLP", Status = WarehouseChangeStatus.PendingPayment,
            Country = "CL", BillOfLadingId = bl.Id, BillOfLading = bl, RequestedByClientId = _org.Id, CreatedAt = DateTime.UtcNow
        });
        _db.WarehouseChangeList.Add(new WarehouseChange
        {
            FromWarehouse = "X", ToWarehouse = "Y", Currency = "CLP", Status = WarehouseChangeStatus.PendingPayment,
            Country = "CL", BillOfLadingId = bl.Id, BillOfLading = bl, RequestedByClientId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow
        });

        var result = await Handler().Handle(new GetDashboardQuery(), CancellationToken.None);

        result.Value.Requests.InProgress.Should().Be(1);
        result.Value.Requests.Items.Should().ContainSingle(r => r.Kind == DashboardTargets.WarehouseChange && r.BlNumber == "BL-CONS");
    }

    [Fact]
    public async Task Invoices_ShouldIncludePendingInvoicesOfTheOrganizationOnly()
    {
        CustomerInvoice Invoice(Guid organizationId, string number, string status) => new()
        {
            OrganizationId = organizationId, SourceNumber = number, DocumentType = InvoiceDocumentTypes.Invoice,
            IssueDate = new DateOnly(2026, 9, 1), DueDate = new DateOnly(2026, 9, 15), LegalName = "Org", TaxId = "1-9",
            TotalAmount = 1000m, Currency = "CLP", Status = status, IsPayable = true, Country = "CL", Source = "TEST"
        };
        _db.CustomerInvoiceList.Add(Invoice(_org.Id, "INV-OWN", InvoiceStatus.Pending));
        _db.CustomerInvoiceList.Add(Invoice(_org.Id, "INV-PAID", InvoiceStatus.Paid));
        _db.CustomerInvoiceList.Add(Invoice(Guid.NewGuid(), "INV-FOREIGN", InvoiceStatus.Pending));

        var result = await Handler().Handle(new GetDashboardQuery(), CancellationToken.None);

        result.Value.PendingPayments.Items.Should().ContainSingle(i => i.ItemType == PayableItemTypes.Invoice)
            .Which.Status.Should().Be(InvoiceStatus.Overdue);
    }
}
