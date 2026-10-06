namespace HapagPortal.UnitTests.Application.ThirdPartyAccess;

using FluentAssertions;
using HapagPortal.Application.BillsOfLading.Import;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Application.ThirdPartyAccess.Defaults;
using HapagPortal.Application.ThirdPartyAccess.Grants;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>Terceros por defecto sobre BL nuevos (M1-13) y acceso anticipado por booking con reconciliación (M1-20).</summary>
public sealed class DefaultsAndBookingAccessTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly FakeNotificationPublisher _notifications = new();
    private readonly ThirdPartyTestData.Actor _customer;
    private readonly ThirdPartyTestData.Actor _agency;
    private readonly ThirdPartyTestData.Actor _consignee;

    public DefaultsAndBookingAccessTests()
    {
        _customer = ThirdPartyTestData.Organization(_db, name: "Customer");
        _agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency, name: "Agency");
        _consignee = ThirdPartyTestData.Organization(_db, name: "Consignee");
        _consignee.Organization.Country = CountryCodes.Bolivia;
        _consignee.Organization.TaxId = "1023456017";
    }

    private ImportBillRow Row(string blNumber, string? bookingNumber = null, string? consigneeTaxId = null) => new(
        ClientId: _customer.Organization.Id,
        BLNumber: blNumber,
        ShipmentType: "Import",
        Country: CountryCodes.Bolivia,
        PortOfLoading: "CNSHA",
        PortOfDischarge: "CLARI",
        FreightCurrency: "USD",
        ConsigneeName: consigneeTaxId is null ? null : "Consignee SRL",
        ConsigneeTaxId: consigneeTaxId,
        HsCode: "870323",
        GrossWeight: 1000m,
        ContainerNumber: null,
        ContainerIsoType: null,
        BookingNumber: bookingNumber);

    private async Task<DefaultGranteeDto> AddDefaultAsync(IReadOnlyList<string>? actionCodes = null, int? days = null)
    {
        var result = await new CreateDefaultGranteeCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db))
            .Handle(new CreateDefaultGranteeCommand(_agency.Organization.Id, actionCodes, days), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task NewBl_ShouldGrantAccessToDefaultGrantees_WithConfiguredPermissionsAndDuration()
    {
        var configured = await AddDefaultAsync([ShipmentActionCodes.PayImportDemurrage], days: 90);

        var import = await new ImportBillsOfLadingCommandHandler(_db)
            .Handle(new ImportBillsOfLadingCommand([Row("BL-NEW-1")]), CancellationToken.None);

        import.Value.Created.Should().Be(1);
        var grant = _db.AccessGrantList.Should().ContainSingle().Subject;
        grant.GrantType.Should().Be(AccessGrantTypes.Default);
        grant.DefaultGranteeId.Should().Be(configured.Id);
        grant.GranteeClientId.Should().Be(_agency.Organization.Id);
        grant.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddDays(90), TimeSpan.FromMinutes(1));

        // Demurrage es X para el Customer: por esta vía tampoco se otorga más de lo que el otorgante posee.
        ActionCodeList.Parse(grant.ActionCodes).Should().Equal(ShipmentActionCodes.ViewShipment);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.GrantCreated && e.AccessGrantId == grant.Id);

        var agencyEvaluator = _agency.Evaluator(_db);
        var bl = _db.BillsOfLadingList.Single();
        (await agencyEvaluator.EvaluateAsync(await agencyEvaluator.GetScopeAsync(), bl)).Can(ShipmentActionCodes.ViewShipment)
            .Should().BeTrue();
    }

    [Fact]
    public async Task ChangingDefaults_ShouldNotTouchExistingGrants()
    {
        var configured = await AddDefaultAsync();
        await new ImportBillsOfLadingCommandHandler(_db).Handle(new ImportBillsOfLadingCommand([Row("BL-NEW-2")]), CancellationToken.None);
        var grant = _db.AccessGrantList.Single();

        var update = await new UpdateDefaultGranteeCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db))
            .Handle(new UpdateDefaultGranteeCommand(configured.Id, [ShipmentActionCodes.ViewTracking], 10), CancellationToken.None);
        var remove = await new RemoveDefaultGranteeCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db))
            .Handle(new RemoveDefaultGranteeCommand(configured.Id), CancellationToken.None);

        update.IsSuccess.Should().BeTrue();
        remove.IsSuccess.Should().BeTrue();
        grant.ActionCodes.Should().BeNull();
        grant.Status.Should().Be(AccessGrantStatus.Active);
        _db.AccessAuditEntryList.Select(e => e.EventType).Should().Contain(
            [AccessAuditEvents.DefaultGranteeAdded, AccessAuditEvents.DefaultGranteeUpdated, AccessAuditEvents.DefaultGranteeRemoved]);

        // Ya no se aplica a los BL siguientes.
        await new ImportBillsOfLadingCommandHandler(_db).Handle(new ImportBillsOfLadingCommand([Row("BL-NEW-3")]), CancellationToken.None);
        _db.AccessGrantList.Should().ContainSingle();
    }

    [Fact]
    public async Task DuplicateDefault_ShouldFail()
    {
        await AddDefaultAsync();

        var result = await new CreateDefaultGranteeCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db))
            .Handle(new CreateDefaultGranteeCommand(_agency.Organization.Id), CancellationToken.None);

        result.Error.Code.Should().Be("DefaultGrantee.AlreadyExists");
    }

    [Fact]
    public async Task CustomsAgency_ShouldNotConfigureDefaults()
    {
        var result = await new CreateDefaultGranteeCommandHandler(_db, _agency.CurrentUser, _agency.Evaluator(_db))
            .Handle(new CreateDefaultGranteeCommand(_customer.Organization.Id), CancellationToken.None);

        result.Error.Code.Should().Be("AccessGrant.NotAllowed");
    }

    [Fact]
    public async Task EarlyBookingAccess_ShouldBeReconciledWithTheOfficialRole_WhenTheBlArrives()
    {
        var early = await new GrantEarlyBookingAccessCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db), _notifications)
            .Handle(new GrantEarlyBookingAccessCommand("BKG-EARLY", _consignee.Organization.Id, ShipmentRoleCodes.Consignee),
                CancellationToken.None);

        early.IsSuccess.Should().BeTrue();
        var grant = _db.AccessGrantList.Single();
        grant.BillOfLadingId.Should().BeNull();
        grant.IntendedRole.Should().Be(ShipmentRoleCodes.Consignee);
        _notifications.Published.Should().ContainSingle(n => n.UserId == _consignee.User.Id);

        await new ImportBillsOfLadingCommandHandler(_db).Handle(
            new ImportBillsOfLadingCommand([Row("BL-EARLY", "BKG-EARLY", _consignee.Organization.TaxId)]), CancellationToken.None);

        var bl = _db.BillsOfLadingList.Single();
        grant.BillOfLadingId.Should().Be(bl.Id);
        grant.Status.Should().Be(AccessGrantStatus.Reconciled);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.BookingAccessLinked);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.BookingAccessReconciled
            && e.Details!.Contains("ReplacedByOfficialRole"));

        // Sin pérdida de visibilidad: ahora la ve por su rol oficial.
        var evaluator = _consignee.Evaluator(_db);
        var permissions = await evaluator.EvaluateAsync(await evaluator.GetScopeAsync(), bl);
        permissions.Roles.Should().Equal(ShipmentRoleCodes.Consignee);
        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
    }

    [Fact]
    public async Task EarlyBookingAccess_WhenTheOfficialRoleDiffers_ShouldBeKeptAndLinked()
    {
        await new GrantEarlyBookingAccessCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db), _notifications)
            .Handle(new GrantEarlyBookingAccessCommand("BKG-KEEP", _agency.Organization.Id, ShipmentRoleCodes.Shipper), CancellationToken.None);

        await new ImportBillsOfLadingCommandHandler(_db).Handle(
            new ImportBillsOfLadingCommand([Row("BL-KEEP", "BKG-KEEP")]), CancellationToken.None);

        var grant = _db.AccessGrantList.Single(g => g.GrantType == AccessGrantTypes.EarlyBooking);
        var bl = _db.BillsOfLadingList.Single();
        grant.Status.Should().Be(AccessGrantStatus.Active);
        grant.BillOfLadingId.Should().Be(bl.Id);

        var evaluator = _agency.Evaluator(_db);
        (await evaluator.EvaluateAsync(await evaluator.GetScopeAsync(), bl)).Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
    }

    [Fact]
    public async Task EarlyBookingAccess_ShouldNotLinkToAnotherCustomersBl()
    {
        await new GrantEarlyBookingAccessCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db), _notifications)
            .Handle(new GrantEarlyBookingAccessCommand("BKG-OTHER", _agency.Organization.Id), CancellationToken.None);

        var other = ThirdPartyTestData.Organization(_db, name: "Other");
        await new ImportBillsOfLadingCommandHandler(_db).Handle(
            new ImportBillsOfLadingCommand([Row("BL-OTHER", "BKG-OTHER") with { ClientId = other.Organization.Id }]), CancellationToken.None);

        _db.AccessGrantList.Single(g => g.GrantType == AccessGrantTypes.EarlyBooking).BillOfLadingId.Should().BeNull();
    }

    [Theory]
    [InlineData(OrganizationTypes.Customer, false)]
    [InlineData(OrganizationTypes.FreightForwarder, true)]
    public async Task EarlyBookingAccess_AsThirdParty_ShouldOnlyBeReceivedByAFreightForwarder(string granteeType, bool allowed)
    {
        var grantee = ThirdPartyTestData.Organization(_db, granteeType);

        var result = await new GrantEarlyBookingAccessCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db), _notifications)
            .Handle(new GrantEarlyBookingAccessCommand("BKG-FF", grantee.Organization.Id, ShipmentRoleCodes.ThirdParty),
                CancellationToken.None);

        result.IsSuccess.Should().Be(allowed);
        if (!allowed)
            result.Error.Code.Should().Be("AccessGrant.EarlyBookingRecipient");
    }

    [Fact]
    public async Task EarlyBookingAccess_ShouldOnlyBeGrantedByTheCustomer()
    {
        var bl = AccessTestData.AddBl(_db, Guid.NewGuid(), "BL-SHIP", bookingNumber: "BKG-SHIP");
        AccessTestData.AddRole(_db, bl, _customer.Organization, ShipmentRoleCodes.Shipper);

        var result = await new GrantEarlyBookingAccessCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db), _notifications)
            .Handle(new GrantEarlyBookingAccessCommand("BKG-SHIP", _agency.Organization.Id), CancellationToken.None);

        result.Error.Code.Should().Be("AccessGrant.EarlyBookingNotAllowed");
    }
}
