namespace HapagPortal.UnitTests.Application.ThirdPartyAccess;

using FluentAssertions;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using Microsoft.EntityFrameworkCore;

/// <summary>Evaluador de accesos con accesos otorgados, vigencia, acceso abierto, autoasociación y ampliaciones (Ola B).</summary>
public sealed class ThirdPartyAccessEvaluatorTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly ThirdPartyTestData.Actor _owner;
    private readonly BillOfLading _bl;

    public ThirdPartyAccessEvaluatorTests()
    {
        _owner = ThirdPartyTestData.Organization(_db, name: "Owner");
        _bl = AccessTestData.AddBl(_db, _owner.Organization.Id, "BL-OWNED", bookingNumber: "BKG-1");
        AccessTestData.AddRole(_db, _bl, _owner.Organization, ShipmentRoleCodes.Consignee);
    }

    [Fact]
    public async Task ExplicitGrant_ShouldTurnOnGrantIntoAllowed_AndKeepTheRestDenied()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, agency.Organization, _bl,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayImportDemurrage]);

        var evaluator = agency.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();
        var visible = await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync();
        var permissions = await evaluator.EvaluateAsync(scope, _bl);

        visible.Should().ContainSingle(b => b.Id == _bl.Id);
        permissions.AccessSource.Should().Be(ShipmentAccessSources.Grant);
        permissions.Roles.Should().Equal(ShipmentRoleCodes.CustomsAgency);
        permissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeTrue();          // X (o) otorgado
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();                 // X (o) no otorgado
        permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges).Should().BeFalse();   // O retirado por el conjunto explícito
        permissions.GrantFor(ShipmentActionCodes.PayImportDemurrage).Should().NotBeNull();
    }

    [Fact]
    public async Task GrantWithoutExplicitSet_ShouldApplyBaseLevel_UnderGrantorCeiling()
    {
        var customer = ThirdPartyTestData.Organization(_db);
        // Techo: el otorgante no tiene TATC (Customer: X) aunque el tercero tenga O en su columna.
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, customer.Organization, _bl,
            actionCodes: null,
            ceiling: _db.ShipmentActionList.Select(a => a.Code).Where(c => c != ShipmentActionCodes.DownloadTatc));

        var evaluator = customer.Evaluator(_db);
        var permissions = await evaluator.EvaluateAsync(await evaluator.GetScopeAsync(), _bl);

        permissions.Roles.Should().Equal(ShipmentRoleCodes.ThirdParty);
        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges).Should().BeTrue();    // O del tercero
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();                 // X (o) sin otorgar
        permissions.Can(ShipmentActionCodes.DownloadTatc).Should().BeFalse();               // fuera del techo
    }

    [Fact]
    public async Task ExpiredGrant_ShouldBeDeniedAtQueryTime_WithoutWaitingForTheJob()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, agency.Organization, _bl,
            validFrom: DateTime.UtcNow.AddDays(-10), validTo: DateTime.UtcNow.AddMinutes(-1));

        var evaluator = agency.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();

        (await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync()).Should().BeEmpty();
        (await evaluator.EvaluateAsync(scope, _bl)).Can(ShipmentActionCodes.ViewShipment).Should().BeFalse();
    }

    [Theory]
    [InlineData(AccessGrantStatus.Revoked)]
    [InlineData(AccessGrantStatus.Expired)]
    [InlineData(AccessGrantStatus.PendingAcceptance)]
    public async Task NonActiveGrant_ShouldGiveNoAccess(string status)
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, agency.Organization, _bl, status: status);

        var evaluator = agency.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();

        (await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync()).Should().BeEmpty();
        (await evaluator.EvaluateAsync(scope, _bl)).AccessSource.Should().Be(ShipmentAccessSources.None);
    }

    [Fact]
    public async Task FutureGrant_ShouldNotApplyBeforeValidFrom()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, agency.Organization, _bl, validFrom: DateTime.UtcNow.AddDays(2));

        var evaluator = agency.Evaluator(_db);
        (await evaluator.FilterAccessible(_db.BillsOfLading, await evaluator.GetScopeAsync()).ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task OpenAccess_ShouldShowOnlyByExactNumber_WithItsSinglePermissionSet()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        ThirdPartyTestData.EnableOpenAccess(_db, _owner.Organization,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.ViewTracking, ShipmentActionCodes.PayMandatoryLocalCharges]);

        var evaluator = agency.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();

        (await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync()).Should().BeEmpty(); // no lista
        (await evaluator.FilterOpenAccess(_db.BillsOfLading, scope).ToListAsync()).Should().ContainSingle();

        var permissions = await evaluator.EvaluateAsync(scope, _bl);
        permissions.AccessSource.Should().Be(ShipmentAccessSources.OpenAccess);
        permissions.Can(ShipmentActionCodes.ViewTracking).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeFalse();
        permissions.CanSelfAssociate.Should().BeTrue();
        permissions.RequiresAssociationForPayment.Should().BeTrue();
    }

    [Fact]
    public async Task OpenAccess_ShouldNeverIncludeAccessAdministration()
    {
        var customer = ThirdPartyTestData.Organization(_db);
        ThirdPartyTestData.EnableOpenAccess(_db, _owner.Organization, actionCodes: null);

        var evaluator = customer.Evaluator(_db);
        var permissions = await evaluator.EvaluateAsync(await evaluator.GetScopeAsync(), _bl);

        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.GrantAccess).Should().BeFalse();
        permissions.Can(ShipmentActionCodes.ExtendDataVisibility).Should().BeFalse();
    }

    [Fact]
    public async Task OpenAccessDisabled_ShouldGiveNoAccessAndNotRevealTheBl()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        ThirdPartyTestData.EnableOpenAccess(_db, _owner.Organization, enabled: false);

        var evaluator = agency.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();

        (await evaluator.FilterOpenAccess(_db.BillsOfLading, scope).ToListAsync()).Should().BeEmpty();
        (await evaluator.EvaluateAsync(scope, _bl)).HasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task SelfAssociation_ShouldListTheBl_WhileOpenAccessStaysActive()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        var setting = ThirdPartyTestData.EnableOpenAccess(_db, _owner.Organization);
        _db.ShipmentAssociationList.Add(new ShipmentAssociation
        {
            BillOfLadingId = _bl.Id,
            ClientId = agency.Organization.Id,
            AssociatedByUserId = agency.User.Id,
            AssociatedAt = DateTime.UtcNow
        });

        var evaluator = agency.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, _bl);

        (await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync()).Should().ContainSingle();
        permissions.AccessSource.Should().Be(ShipmentAccessSources.SelfAssociated);
        permissions.CanSelfAssociate.Should().BeFalse();
        permissions.RequiresAssociationForPayment.Should().BeFalse();

        setting.IsEnabled = false;
        var later = agency.Evaluator(_db);
        (await later.FilterAccessible(_db.BillsOfLading, await later.GetScopeAsync()).ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Widening_ShouldGiveTheTargetRoleTheWidenedData_UntilRevoked()
    {
        var shipper = ThirdPartyTestData.Organization(_db);
        AccessTestData.AddRole(_db, _bl, shipper.Organization, ShipmentRoleCodes.Shipper);
        var widening = new VisibilityWidening
        {
            BillOfLadingId = _bl.Id,
            GrantorClientId = _owner.Organization.Id,
            GrantorRole = ShipmentRoleCodes.Customer,
            TargetRole = ShipmentRoleCodes.Shipper,
            ActionCode = ShipmentActionCodes.PayFreight,
            Status = VisibilityWideningStatus.Active
        };
        _db.VisibilityWideningList.Add(widening);

        var evaluator = shipper.Evaluator(_db);
        (await evaluator.EvaluateAsync(await evaluator.GetScopeAsync(), _bl)).Can(ShipmentActionCodes.PayFreight).Should().BeTrue();

        widening.Status = VisibilityWideningStatus.Revoked;
        var later = shipper.Evaluator(_db);
        (await later.EvaluateAsync(await later.GetScopeAsync(), _bl)).Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
    }

    [Fact]
    public async Task EarlyBookingGrant_ShouldEvaluateTheGranteeWithTheIntendedRole()
    {
        var futureShipper = ThirdPartyTestData.Organization(_db);
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, futureShipper.Organization, _bl,
            grantType: AccessGrantTypes.EarlyBooking, intendedRole: ShipmentRoleCodes.Shipper);

        var evaluator = futureShipper.Evaluator(_db);
        var permissions = await evaluator.EvaluateAsync(await evaluator.GetScopeAsync(), _bl);

        permissions.Roles.Should().Equal(ShipmentRoleCodes.Shipper);
        permissions.Can(ShipmentActionCodes.RequestUnvaluedBlCopy).Should().BeTrue();  // O para Shipper, X (o) para Tercero
    }

    [Fact]
    public async Task OwnRoles_ShouldNotDependOnGrants_AndGrantForShouldBeNull()
    {
        var evaluator = _owner.Evaluator(_db);
        var permissions = await evaluator.EvaluateAsync(await evaluator.GetScopeAsync(), _bl);

        permissions.AccessSource.Should().Be(ShipmentAccessSources.Own);
        permissions.GrantFor(ShipmentActionCodes.PayFreight).Should().BeNull();
    }

    [Fact]
    public async Task SearchList_ShouldShowGrantSource()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, agency.Organization, _bl);

        var evaluator = agency.Evaluator(_db);
        var handler = new HapagPortal.Application.Shipments.Search.SearchShipmentsQueryHandler(_db, evaluator);
        var result = await handler.Handle(new HapagPortal.Application.Shipments.Search.SearchShipmentsQuery(), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(i => i.BlNumber == _bl.BLNumber && i.AccessSource == ShipmentAccessSources.Grant);
    }
}
