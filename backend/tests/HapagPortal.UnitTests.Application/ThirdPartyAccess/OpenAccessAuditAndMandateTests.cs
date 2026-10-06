namespace HapagPortal.UnitTests.Application.ThirdPartyAccess;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Create;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Application.ThirdPartyAccess.Audit;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Application.ThirdPartyAccess.OpenAccess;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>Acceso abierto y autoasociación (M1-17, M1-18), auditoría (M1-23) y pagos bajo mandato (NF-14).</summary>
public sealed class OpenAccessAuditAndMandateTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly ThirdPartyTestData.Actor _owner;
    private readonly ThirdPartyTestData.Actor _agency;
    private readonly BillOfLading _bl;

    public OpenAccessAuditAndMandateTests()
    {
        _owner = ThirdPartyTestData.Organization(_db, name: "Owner");
        _agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency, name: "Agency");
        _bl = AccessTestData.AddBl(_db, _owner.Organization.Id, "BL-OPEN", bookingNumber: "BKG-OPEN");
        AccessTestData.AddRole(_db, _bl, _owner.Organization, ShipmentRoleCodes.Consignee);
    }

    private Task<Result<OpenAccessSettingDto>> SetOpenAccessAsync(ThirdPartyTestData.Actor actor, bool enabled, IReadOnlyList<string>? codes = null) =>
        new UpdateOpenAccessSettingCommandHandler(_db, actor.CurrentUser, actor.Evaluator(_db))
            .Handle(new UpdateOpenAccessSettingCommand(enabled, codes), CancellationToken.None);

    [Fact]
    public async Task OpenAccessToggle_ShouldBeAudited_AndRestrictedToCustomers()
    {
        var enabled = await SetOpenAccessAsync(_owner, true, [ShipmentActionCodes.ViewTracking]);
        var disabled = await SetOpenAccessAsync(_owner, false);
        var byAgency = await SetOpenAccessAsync(_agency, true);

        enabled.IsSuccess.Should().BeTrue();
        enabled.Value.EffectiveActionCodes.Should().BeEquivalentTo([ShipmentActionCodes.ViewShipment, ShipmentActionCodes.ViewTracking]);
        disabled.Value.IsEnabled.Should().BeFalse();
        disabled.Value.ActionCodes.Should().NotBeNull();   // el conjunto se conserva
        byAgency.Error.Code.Should().Be("OpenAccess.NotAllowed");

        _db.AccessAuditEntryList.Select(e => e.EventType).Should().Equal(
            AccessAuditEvents.OpenAccessEnabled,
            AccessAuditEvents.OpenAccessPermissionsChanged,
            AccessAuditEvents.OpenAccessDisabled);
        _db.AccessAuditEntryList.Should().OnlyContain(e => e.ActorUserId == _owner.User.Id && e.GrantorClientId == _owner.Organization.Id);
    }

    [Fact]
    public async Task Detail_ByExactNumberWithOpenAccess_ShouldShowAllowedInfoAndOfferAssociation()
    {
        await SetOpenAccessAsync(_owner, true, [ShipmentActionCodes.PayMandatoryLocalCharges]);

        var detail = await new GetShipmentDetailQueryHandler(_db, _agency.Evaluator(_db))
            .Handle(new GetShipmentDetailQuery(_bl.BLNumber), CancellationToken.None);

        detail.IsSuccess.Should().BeTrue();
        detail.Value.AccessSource.Should().Be(ShipmentAccessSources.OpenAccess);
        detail.Value.CanSelfAssociate.Should().BeTrue();
        detail.Value.RequiresAssociationForPayment.Should().BeTrue();
        detail.Value.Freight.Should().BeNull();
        detail.Value.LocalCharges.Should().NotBeNull();
    }

    [Fact]
    public async Task Detail_WithoutOpenAccess_ShouldNotRevealTheBl()
    {
        var detail = await new GetShipmentDetailQueryHandler(_db, _agency.Evaluator(_db))
            .Handle(new GetShipmentDetailQuery(_bl.BLNumber), CancellationToken.None);

        detail.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task SelfAssociation_ShouldPersist_AppearInTheList_AndBeAudited()
    {
        await SetOpenAccessAsync(_owner, true);

        var associate = new SelfAssociateShipmentCommandHandler(_db, _agency.CurrentUser, _agency.Evaluator(_db));
        var result = await associate.Handle(new SelfAssociateShipmentCommand(_bl.BLNumber), CancellationToken.None);
        var again = await new SelfAssociateShipmentCommandHandler(_db, _agency.CurrentUser, _agency.Evaluator(_db))
            .Handle(new SelfAssociateShipmentCommand(_bl.BLNumber), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        again.Error.Code.Should().Be("ShipmentAssociation.AlreadyExists");
        _db.ShipmentAssociationList.Should().ContainSingle(a => a.ClientId == _agency.Organization.Id && a.AssociatedByUserId == _agency.User.Id);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.SelfAssociated && e.GranteeClientId == _agency.Organization.Id);

        var list = await new SearchShipmentsQueryHandler(_db, _agency.Evaluator(_db)).Handle(new SearchShipmentsQuery(), CancellationToken.None);
        list.Value.Items.Should().ContainSingle(i => i.BlNumber == _bl.BLNumber && i.AccessSource == ShipmentAccessSources.SelfAssociated);
    }

    [Fact]
    public async Task SelfAssociation_WithoutOpenAccess_ShouldNotBeAvailable()
    {
        var result = await new SelfAssociateShipmentCommandHandler(_db, _agency.CurrentUser, _agency.Evaluator(_db))
            .Handle(new SelfAssociateShipmentCommand(_bl.BLNumber), CancellationToken.None);

        result.Error.Code.Should().Be("BillOfLading.NotFound");
        _db.ShipmentAssociationList.Should().BeEmpty();
    }

    [Fact]
    public async Task Audit_ShouldShowTheWholeHistoryToTheParties_AndOnlyTheirOwnEventsToThirdParties()
    {
        var customer = ThirdPartyTestData.Organization(_db, name: "Third party");
        var other = ThirdPartyTestData.Organization(_db, name: "Other third party");
        var mine = ThirdPartyTestData.AddGrant(_db, _owner.Organization, customer.Organization, _bl);
        var theirs = ThirdPartyTestData.AddGrant(_db, _owner.Organization, other.Organization, _bl);
        _db.AccessAuditEntryList.Add(new AccessAuditEntry
        {
            EventType = AccessAuditEvents.GrantCreated, OccurredAt = DateTime.UtcNow, BillOfLadingId = _bl.Id,
            AccessGrantId = mine.Id, GrantorClientId = _owner.Organization.Id, GranteeClientId = customer.Organization.Id
        });
        _db.AccessAuditEntryList.Add(new AccessAuditEntry
        {
            EventType = AccessAuditEvents.GrantCreated, OccurredAt = DateTime.UtcNow, BillOfLadingId = _bl.Id,
            AccessGrantId = theirs.Id, GrantorClientId = _owner.Organization.Id, GranteeClientId = other.Organization.Id
        });

        var ownerView = await new GetAccessAuditQueryHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db))
            .Handle(new GetAccessAuditQuery(BlNumber: _bl.BLNumber), CancellationToken.None);
        var thirdPartyView = await new GetAccessAuditQueryHandler(_db, customer.CurrentUser, customer.Evaluator(_db))
            .Handle(new GetAccessAuditQuery(BookingNumber: _bl.BookingNumber), CancellationToken.None);

        ownerView.Value.Total.Should().Be(2);
        thirdPartyView.Value.Items.Should().ContainSingle(e => e.AccessGrantId == mine.Id);
    }

    [Fact]
    public async Task Audit_ShouldBeForbiddenWhereTheMatrixDeniesIt()
    {
        // Agencia de aduanas: "consultar la auditoría de accesos" es X.
        ThirdPartyTestData.AddGrant(_db, _owner.Organization, _agency.Organization, _bl);

        var result = await new GetAccessAuditQueryHandler(_db, _agency.CurrentUser, _agency.Evaluator(_db))
            .Handle(new GetAccessAuditQuery(BlNumber: _bl.BLNumber), CancellationToken.None);

        result.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task PaymentUnderMandate_ShouldIdentifyMandatorAndMandatary()
    {
        var mandate = ThirdPartyTestData.AddGrant(_db, _owner.Organization, _agency.Organization, _bl,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayFreight], isMandate: true);

        var handler = new CreatePaymentCommandHandler(
            _db, Substitute.For<IPaymentGatewayService>(), _agency.CurrentUser, _agency.Evaluator(_db));
        var result = await handler.Handle(new CreatePaymentCommand(_bl.Id, "Freight", "Cash", null, null, "CL"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var payment = _db.PaymentList.Single();
        payment.ClientId.Should().Be(_agency.Organization.Id);            // mandatario que ejecutó
        payment.OnBehalfOfClientId.Should().Be(_owner.Organization.Id);   // mandante
        payment.AccessGrantId.Should().Be(mandate.Id);
    }

    [Fact]
    public async Task OwnPayment_ShouldNotReferenceAnyGrant()
    {
        var handler = new CreatePaymentCommandHandler(
            _db, Substitute.For<IPaymentGatewayService>(), _owner.CurrentUser, _owner.Evaluator(_db));
        var result = await handler.Handle(new CreatePaymentCommand(_bl.Id, "Freight", "Cash", null, null, "CL"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _db.PaymentList.Single().OnBehalfOfClientId.Should().BeNull();
        _db.PaymentList.Single().AccessGrantId.Should().BeNull();
    }
}
