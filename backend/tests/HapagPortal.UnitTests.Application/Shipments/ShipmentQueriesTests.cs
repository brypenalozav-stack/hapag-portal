namespace HapagPortal.UnitTests.Application.Shipments;

using HapagPortal.Application.Common.Interfaces;
using NSubstitute;
using HapagPortal.Application.ChargeRules.Common;
using FluentAssertions;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class ShipmentQueriesTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly Client _org;
    private readonly SearchShipmentsQueryHandler _search;
    private readonly GetShipmentDetailQueryHandler _detail;
    private readonly IShipmentAccessEvaluator _evaluator;

    public ShipmentQueriesTests()
    {
        var client = AccessTestData.ClientContext(_db);
        _org = client.Organization;
        _evaluator = client.Evaluator;
        _search = new SearchShipmentsQueryHandler(_db, client.Evaluator);
        _detail = new GetShipmentDetailQueryHandler(_db, client.Evaluator, Substitute.For<IChargeRulesService>());
    }

    private BillOfLading Own(string blNumber, string role, string type = "Import", string country = "CL",
        string? booking = null, string? vessel = "Hamburg Express", string? voyage = "025E", string status = "Active")
    {
        var bl = AccessTestData.AddBl(_db, Guid.NewGuid(), blNumber, type, country, booking, vessel, voyage, status);
        AccessTestData.AddRole(_db, bl, _org, role);
        return bl;
    }

    [Fact]
    public async Task Search_ShouldListOwnByRoleAndExcludeOtherOrganizations()
    {
        Own("BL-CONS", ShipmentRoleCodes.Consignee);
        AccessTestData.AddBl(_db, _org.Id, "BL-CUST");                     // titular -> Customer
        Own("BL-SHIP", ShipmentRoleCodes.Shipper, type: "Export");
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-FOREIGN");

        var result = await _search.Handle(new SearchShipmentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(3);
        result.Value.Items.Select(i => i.BlNumber).Should().BeEquivalentTo(["BL-CONS", "BL-CUST", "BL-SHIP"]);
        result.Value.Items.Single(i => i.BlNumber == "BL-SHIP").Roles.Should().Equal(ShipmentRoleCodes.Shipper);
        result.Value.Items.Single(i => i.BlNumber == "BL-CUST").Roles.Should().Equal(ShipmentRoleCodes.Customer);
        result.Value.Items.Should().OnlyContain(i => i.AccessSource == ShipmentAccessSources.Own);
    }

    [Fact]
    public async Task Search_OperationFilter_ShouldSeparateImportAndExport()
    {
        Own("BL-IMP", ShipmentRoleCodes.Consignee, type: "Import");
        Own("BL-EXP", ShipmentRoleCodes.Shipper, type: "EXPORT");

        var exports = await _search.Handle(new SearchShipmentsQuery(Operation: "export"), CancellationToken.None);
        var imports = await _search.Handle(new SearchShipmentsQuery(Operation: "IMPORT"), CancellationToken.None);

        exports.Value.Items.Should().ContainSingle(i => i.BlNumber == "BL-EXP" && i.Operation == ShipmentOperations.Export);
        imports.Value.Items.Should().ContainSingle(i => i.BlNumber == "BL-IMP" && i.Operation == ShipmentOperations.Import);
    }

    [Fact]
    public async Task Search_ColumnFilters_ShouldApply()
    {
        Own("HLCU-AAA-1", ShipmentRoleCodes.Consignee, booking: "BKG-111", vessel: "Berlin Express", voyage: "031W", status: "InTransit", country: "CL");
        Own("HLCU-BBB-2", ShipmentRoleCodes.Consignee, booking: "BKG-222", vessel: "Colombo Express", voyage: "018E", status: "Arrived", country: "BO");

        (await _search.Handle(new SearchShipmentsQuery(BlNumber: "aaa"), CancellationToken.None))
            .Value.Items.Should().ContainSingle(i => i.BlNumber == "HLCU-AAA-1");
        (await _search.Handle(new SearchShipmentsQuery(BookingNumber: "222"), CancellationToken.None))
            .Value.Items.Should().ContainSingle(i => i.BlNumber == "HLCU-BBB-2");
        (await _search.Handle(new SearchShipmentsQuery(Vessel: "berlin"), CancellationToken.None))
            .Value.Items.Should().ContainSingle(i => i.BlNumber == "HLCU-AAA-1");
        (await _search.Handle(new SearchShipmentsQuery(Voyage: "018"), CancellationToken.None))
            .Value.Items.Should().ContainSingle(i => i.BlNumber == "HLCU-BBB-2");
        (await _search.Handle(new SearchShipmentsQuery(Status: "intransit"), CancellationToken.None))
            .Value.Items.Should().ContainSingle(i => i.BlNumber == "HLCU-AAA-1");
        (await _search.Handle(new SearchShipmentsQuery(Country: "BO"), CancellationToken.None))
            .Value.Items.Should().ContainSingle(i => i.BlNumber == "HLCU-BBB-2");
    }

    [Fact]
    public async Task Search_ShouldPage()
    {
        for (var i = 1; i <= 5; i++)
            Own($"BL-P{i}", ShipmentRoleCodes.Consignee);

        var result = await _search.Handle(new SearchShipmentsQuery(Page: 2, PageSize: 2), CancellationToken.None);

        result.Value.Total.Should().Be(5);
        result.Value.Page.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Search_ShouldFlagPendingLocalCharges()
    {
        var bl = Own("BL-PEND", ShipmentRoleCodes.Consignee);
        bl.LocalCharges.Add(new LocalCharge { ChargeType = "THC", Currency = "CLP", Status = "Pending", BillOfLadingId = bl.Id });
        Own("BL-CLEAR", ShipmentRoleCodes.Consignee);

        var result = await _search.Handle(new SearchShipmentsQuery(), CancellationToken.None);

        result.Value.Items.Single(i => i.BlNumber == "BL-PEND").HasPendingCharges.Should().BeTrue();
        result.Value.Items.Single(i => i.BlNumber == "BL-CLEAR").HasPendingCharges.Should().BeFalse();
    }

    [Fact]
    public async Task Search_Admin_ShouldSeeAllWithAdminSource()
    {
        var admin = AccessTestData.AdminContext(_db);
        AccessTestData.AddBl(_db, _org.Id, "BL-1");
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-2");
        var handler = new SearchShipmentsQueryHandler(_db, admin.Evaluator);

        var result = await handler.Handle(new SearchShipmentsQuery(), CancellationToken.None);

        result.Value.Total.Should().Be(2);
        result.Value.Items.Should().OnlyContain(i => i.AccessSource == ShipmentAccessSources.Admin);
    }

    [Fact]
    public async Task Detail_OtherOrganizationsBl_ShouldReturnNotFound()
    {
        var other = AccessTestData.AddOrganization(_db);
        var bl = AccessTestData.AddBl(_db, other.Id, "BL-OTHER");
        AccessTestData.AddRole(_db, bl, other, ShipmentRoleCodes.Consignee);

        var result = await _detail.Handle(new GetShipmentDetailQuery("BL-OTHER"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task Detail_Shipper_ShouldHideFreightAndDemurrage()
    {
        var bl = Own("BL-SHIP", ShipmentRoleCodes.Shipper, type: "Export", booking: "BKG-9");
        bl.LocalCharges.Add(new LocalCharge { ChargeType = "GateIn", Currency = "CLP", Status = "Pending", BillOfLadingId = bl.Id });
        bl.DemurrageCharges.Add(new DemurrageCharge { ContainerNumber = "C1", Currency = "CLP", Status = "Pending", BillOfLadingId = bl.Id });

        var result = await _detail.Handle(new GetShipmentDetailQuery("BL-SHIP"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BookingNumber.Should().Be("BKG-9");
        result.Value.Operation.Should().Be(ShipmentOperations.Export);
        result.Value.Roles.Should().Equal(ShipmentRoleCodes.Shipper);
        result.Value.Freight.Should().BeNull();               // X (o)
        result.Value.DemurrageCharges.Should().BeNull();      // X
        result.Value.LocalCharges.Should().ContainSingle();   // O
        result.Value.AllowedActions.Should().NotContain(ShipmentActionCodes.PayFreight);
        result.Value.CanOperate.Should().BeTrue();
    }

    [Fact]
    public async Task Detail_Consignee_ShouldSeeEverythingAndOwnServiceOrders()
    {
        var bl = Own("BL-CONS", ShipmentRoleCodes.Consignee);
        bl.DemurrageCharges.Add(new DemurrageCharge { ContainerNumber = "C1", Currency = "CLP", Status = "Pending", BillOfLadingId = bl.Id });
        bl.Containers.Add(new BLContainer { ContainerNumber = "C1", ContainerType = "40HC", Status = "Discharged", BillOfLadingId = bl.Id });
        _db.ServiceOrderList.Add(new ServiceOrder { OrderNumber = "SO-1", OrderType = "GateIn", Status = "Pending", Country = "CL", BillOfLadingId = bl.Id, ClientId = _org.Id });
        _db.ServiceOrderList.Add(new ServiceOrder { OrderNumber = "SO-2", OrderType = "GateIn", Status = "Pending", Country = "CL", BillOfLadingId = bl.Id, ClientId = Guid.NewGuid() });

        var result = await _detail.Handle(new GetShipmentDetailQuery("BL-CONS"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Freight.Should().NotBeNull();
        result.Value.DemurrageCharges.Should().ContainSingle();
        result.Value.Containers.Should().ContainSingle();
        result.Value.ServiceOrders.Should().ContainSingle(so => so.OrderNumber == "SO-1");
        result.Value.AllowedActions.Should().Contain(ShipmentActionCodes.PayImportDemurrage);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Detail_IpoShouldBeHiddenOnlyForCreditCustomers(bool ipoExcluded, bool expectIpo)
    {
        var bl = Own("BL-IPO", ShipmentRoleCodes.Consignee);
        bl.LocalCharges.Add(new LocalCharge { ChargeType = "THC", Currency = "CLP", Status = "Pending", BillOfLadingId = bl.Id });
        bl.LocalCharges.Add(new LocalCharge { ChargeType = ChargeConceptCodes.Ipo, Currency = "USD", Status = "Pending", BillOfLadingId = bl.Id });
        var rules = Substitute.For<IChargeRulesService>();
        rules.GetConditionsAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>())
            .Returns(new CommercialConditionsDto(true, "Nexus", _org.TaxId, _org.MatchCode, ipoExcluded, ipoExcluded ? 30 : null,
                Array.Empty<string>(), null, null, false, false, ipoExcluded, null));
        var handler = new GetShipmentDetailQueryHandler(_db, _evaluator, rules);

        var result = await handler.Handle(new GetShipmentDetailQuery("BL-IPO"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LocalCharges!.Any(c => c.ChargeCode == ChargeConceptCodes.Ipo).Should().Be(expectIpo);
        result.Value.LocalCharges!.Should().Contain(c => c.ChargeCode == "THC");
    }
}
