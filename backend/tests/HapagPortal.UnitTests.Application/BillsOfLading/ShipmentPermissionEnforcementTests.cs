namespace HapagPortal.UnitTests.Application.BillsOfLading;

using FluentAssertions;
using HapagPortal.Application.AccessMatrix;
using HapagPortal.Application.BillsOfLading.Read.GetByNumber;
using HapagPortal.Application.BillsOfLading.Read.GetMyBLs;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Demurrage.Read.GetByBL;
using HapagPortal.Application.Payments.Create;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>La matriz de M1-11 se aplica en el servidor en todas las consultas por BL (NF-05).</summary>
public sealed class ShipmentPermissionEnforcementTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly Client _org;
    private readonly User _user;
    private readonly ICurrentUserService _currentUser;
    private readonly IShipmentAccessEvaluator _evaluator;

    public ShipmentPermissionEnforcementTests()
    {
        var client = AccessTestData.ClientContext(_db);
        (_org, _user, _currentUser, _evaluator) = client;
    }

    [Fact]
    public async Task GetByNumber_OtherOrganizationsBl_ShouldReturnNotFound()
    {
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-FOREIGN");

        var result = await new GetBLByNumberQueryHandler(_db, _evaluator)
            .Handle(new GetBLByNumberQuery("BL-FOREIGN"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task GetByNumber_BlLinkedByShipmentRole_ShouldBeVisible()
    {
        var bl = AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-SHIPPER", bookingNumber: "BKG-1");
        AccessTestData.AddRole(_db, bl, _org, ShipmentRoleCodes.Shipper);

        var result = await new GetBLByNumberQueryHandler(_db, _evaluator)
            .Handle(new GetBLByNumberQuery("BL-SHIPPER"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BookingNumber.Should().Be("BKG-1");
    }

    [Fact]
    public async Task GetMyBLs_ShouldIncludeOwnAndRoleLinkedOnly()
    {
        AccessTestData.AddBl(_db, _org.Id, "BL-OWNER");
        var linked = AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-LINKED");
        AccessTestData.AddRole(_db, linked, _org, ShipmentRoleCodes.Consignee);
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-FOREIGN");

        var result = await new GetMyBLsQueryHandler(_db, _currentUser, _evaluator)
            .Handle(new GetMyBLsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(b => b.BLNumber).Should().BeEquivalentTo(["BL-OWNER", "BL-LINKED"]);
    }

    [Fact]
    public async Task Demurrage_CustomerOnly_ShouldBeForbidden()
    {
        AccessTestData.AddBl(_db, _org.Id, "BL-CUST");

        var result = await new GetDemurrageByBLQueryHandler(_db, _evaluator)
            .Handle(new GetDemurrageByBLQuery("BL-CUST"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Forbidden");
    }

    [Fact]
    public async Task Demurrage_OtherOrganizationsBl_ShouldReturnNotFound()
    {
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-FOREIGN");

        var result = await new GetDemurrageByBLQueryHandler(_db, _evaluator)
            .Handle(new GetDemurrageByBLQuery("BL-FOREIGN"), CancellationToken.None);

        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task CreatePayment_ShipperFreight_ShouldBeForbidden()
    {
        var bl = AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-SHIP");
        AccessTestData.AddRole(_db, bl, _org, ShipmentRoleCodes.Shipper);
        var gateway = Substitute.For<IPaymentGatewayService>();
        var handler = new CreatePaymentCommandHandler(_db, gateway, _currentUser, _evaluator);

        var result = await handler.Handle(
            new CreatePaymentCommand(bl.Id, "Freight", "Cash", null, null, "CL"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Forbidden");
        _db.PaymentList.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePayment_ViewerProfile_ShouldBeForbidden()
    {
        var viewer = AccessTestData.AddMember(_db, _org, profile: RoleCodes.OrgViewer);
        var viewerUser = AccessTestData.CurrentUser(viewer); // sin shipments.operate
        var bl = AccessTestData.AddBl(_db, _org.Id, "BL-OWN");
        var handler = new CreatePaymentCommandHandler(
            _db, Substitute.For<IPaymentGatewayService>(), viewerUser, AccessTestData.Evaluator(_db, viewerUser));

        var result = await handler.Handle(
            new CreatePaymentCommand(bl.Id, "Freight", "Cash", null, null, "CL"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Forbidden");
    }

    [Fact]
    public async Task AccessMatrixUpdate_ShouldChangeBaseLevelAndBeReadBack()
    {
        var handler = new UpdateAccessMatrixLevelCommandHandler(_db);

        var result = await handler.Handle(
            new UpdateAccessMatrixLevelCommand(ShipmentActionCodes.PayFreight, ShipmentRoleCodes.Shipper, null, AccessLevels.Allowed),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Levels.Should().Contain(l => l.Role == ShipmentRoleCodes.Shipper && l.OrganizationType == null && l.Level == AccessLevels.Allowed);

        var matrix = await new GetAccessMatrixQueryHandler(_db).Handle(new GetAccessMatrixQuery(), CancellationToken.None);
        matrix.Value.Should().HaveCount(_db.ShipmentActionList.Count);
    }

    [Fact]
    public async Task AccessMatrixUpdate_UnknownAction_ShouldReturnNotFound()
    {
        var result = await new UpdateAccessMatrixLevelCommandHandler(_db).Handle(
            new UpdateAccessMatrixLevelCommand("does.not.exist", ShipmentRoleCodes.Shipper, null, AccessLevels.Allowed),
            CancellationToken.None);

        result.Error.Code.Should().Be("ShipmentAction.NotFound");
    }
}
