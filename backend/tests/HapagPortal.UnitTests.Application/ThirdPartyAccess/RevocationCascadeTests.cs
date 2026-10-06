namespace HapagPortal.UnitTests.Application.ThirdPartyAccess;

using FluentAssertions;
using HapagPortal.Application.ThirdPartyAccess.Grants;
using HapagPortal.Application.ThirdPartyAccess.Widenings;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Revocación manual, vencimiento automático y ajuste de derivados (M1-14, M1-22), con auditoría (M1-23)
/// y notificación a todos los afectados.
/// </summary>
public sealed class RevocationCascadeTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly FakeNotificationPublisher _notifications = new();
    private readonly ThirdPartyTestData.Actor _owner;
    private readonly ThirdPartyTestData.Actor _thirdParty;
    private readonly ThirdPartyTestData.Actor _subGrantee;
    private readonly ThirdPartyTestData.Actor _shipper;
    private readonly BillOfLading _bl;

    public RevocationCascadeTests()
    {
        _owner = ThirdPartyTestData.Organization(_db, name: "Owner");
        _thirdParty = ThirdPartyTestData.Organization(_db, name: "ThirdParty");
        _subGrantee = ThirdPartyTestData.Organization(_db, name: "SubGrantee");
        _shipper = ThirdPartyTestData.Organization(_db, name: "Shipper");

        _bl = AccessTestData.AddBl(_db, _owner.Organization.Id, "BL-CASCADE");
        AccessTestData.AddRole(_db, _bl, _owner.Organization, ShipmentRoleCodes.Consignee);
        AccessTestData.AddRole(_db, _bl, _shipper.Organization, ShipmentRoleCodes.Shipper);
    }

    /// <summary>Owner → ThirdParty (con flete y ampliar) → SubGrantee, y ThirdParty amplía el flete al Shipper.</summary>
    private async Task<AccessGrant> BuildChainAsync()
    {
        var origin = await new GrantAccessCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db), _notifications)
            .Handle(new GrantAccessCommand(
                _thirdParty.Organization.Id,
                BlNumbers: [_bl.BLNumber],
                ValidityType: AccessValidityTypes.UntilDate,
                ValidTo: DateTime.UtcNow.AddDays(10),
                ActionCodes: [ShipmentActionCodes.PayFreight, ShipmentActionCodes.GrantAccess, ShipmentActionCodes.ExtendDataVisibility]),
                CancellationToken.None);
        origin.IsSuccess.Should().BeTrue();

        var derived = await new GrantAccessCommandHandler(_db, _thirdParty.CurrentUser, _thirdParty.Evaluator(_db), _notifications)
            .Handle(new GrantAccessCommand(
                _subGrantee.Organization.Id,
                BlNumbers: [_bl.BLNumber],
                ActionCodes: [ShipmentActionCodes.PayFreight]), CancellationToken.None);
        derived.IsSuccess.Should().BeTrue();

        var widened = await new CreateVisibilityWideningsCommandHandler(_db, _thirdParty.CurrentUser, _thirdParty.Evaluator(_db))
            .Handle(new CreateVisibilityWideningsCommand(_bl.BLNumber, ShipmentRoleCodes.Shipper, [ShipmentActionCodes.PayFreight]),
                CancellationToken.None);
        widened.IsSuccess.Should().BeTrue();

        return _db.AccessGrantList.Single(g => g.GranteeClientId == _thirdParty.Organization.Id);
    }

    [Fact]
    public async Task DerivedGrant_ShouldTrackItsOrigin_AndNeverOutliveIt()
    {
        var origin = await BuildChainAsync();
        var derived = _db.AccessGrantList.Single(g => g.GranteeClientId == _subGrantee.Organization.Id);
        var widening = _db.VisibilityWideningList.Single();

        derived.ParentGrantId.Should().Be(origin.Id);
        derived.GrantorRole.Should().Be(ShipmentRoleCodes.ThirdParty);
        derived.ValidTo.Should().Be(origin.ValidTo);              // indefinido recortado a la vigencia del origen
        widening.OriginGrantId.Should().Be(origin.Id);

        var shipperEvaluator = _shipper.Evaluator(_db);
        (await shipperEvaluator.EvaluateAsync(await shipperEvaluator.GetScopeAsync(), _bl))
            .Can(ShipmentActionCodes.PayFreight).Should().BeTrue();
    }

    [Fact]
    public async Task RevokingTheOrigin_ShouldRevokeDerivedGrantsAndWidenings_AndNotifyAffected()
    {
        var origin = await BuildChainAsync();
        _notifications.Published.Clear();

        var result = await new RevokeAccessGrantsCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db), _notifications)
            .Handle(new RevokeAccessGrantsCommand([origin.Id], "Fin de la relación"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new { Revoked = 1, CascadeRevoked = 1, WideningsRevoked = 1 });

        origin.Status.Should().Be(AccessGrantStatus.Revoked);
        origin.EndReason.Should().Be(AccessEndReasons.Manual);
        var derived = _db.AccessGrantList.Single(g => g.GranteeClientId == _subGrantee.Organization.Id);
        derived.Status.Should().Be(AccessGrantStatus.Revoked);
        derived.EndReason.Should().Be(AccessEndReasons.Cascade);
        _db.VisibilityWideningList.Single().Status.Should().Be(VisibilityWideningStatus.Revoked);

        _db.AccessAuditEntryList.Select(e => e.EventType).Should().Contain(
        [
            AccessAuditEvents.GrantRevoked,
            AccessAuditEvents.GrantRevokedByCascade,
            AccessAuditEvents.WideningRevokedByCascade
        ]);

        // No solo quien originó: el tercero directo, el derivado y el rol que perdió la ampliación.
        _notifications.Published.Should().Contain(n => n.Type == NotificationTypes.AccessRevoked && n.UserId == _thirdParty.User.Id);
        _notifications.Published.Should().Contain(n => n.Type == NotificationTypes.AccessRevokedByCascade && n.UserId == _subGrantee.User.Id);
        _notifications.Published.Should().Contain(n => n.Type == NotificationTypes.AccessRevokedByCascade && n.UserId == _shipper.User.Id);

        var subEvaluator = _subGrantee.Evaluator(_db);
        (await subEvaluator.EvaluateAsync(await subEvaluator.GetScopeAsync(), _bl)).HasAccess.Should().BeFalse();
        var shipperEvaluator = _shipper.Evaluator(_db);
        (await shipperEvaluator.EvaluateAsync(await shipperEvaluator.GetScopeAsync(), _bl))
            .Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
    }

    [Fact]
    public async Task OnlyTheGrantor_ShouldRevoke()
    {
        var origin = await BuildChainAsync();

        var result = await new RevokeAccessGrantsCommandHandler(_db, _subGrantee.CurrentUser, _subGrantee.Evaluator(_db), _notifications)
            .Handle(new RevokeAccessGrantsCommand([origin.Id]), CancellationToken.None);

        result.Error.Code.Should().Be("AccessGrant.NotFound");
        origin.Status.Should().Be(AccessGrantStatus.Active);
    }

    [Fact]
    public async Task Expiry_ShouldBeRecordedAutomatically_AndCascade()
    {
        var origin = await BuildChainAsync();
        origin.ValidTo = DateTime.UtcNow.AddMinutes(-1);
        _db.AccessGrantList.Single(g => g.ParentGrantId == origin.Id).ValidTo = origin.ValidTo;
        _notifications.Published.Clear();

        var result = await new ExpireAccessGrantsCommandHandler(_db, _notifications)
            .Handle(new ExpireAccessGrantsCommand(), CancellationToken.None);

        result.Value.Should().Be(2);
        origin.Status.Should().Be(AccessGrantStatus.Expired);
        origin.EndReason.Should().Be(AccessEndReasons.Expired);
        origin.EndedByUserId.Should().BeNull();
        _db.VisibilityWideningList.Single().EndReason.Should().Be(AccessEndReasons.Cascade);
        _db.AccessAuditEntryList.Where(e => e.EventType == AccessAuditEvents.GrantExpired)
            .Should().HaveCount(2).And.OnlyContain(e => e.ActorUserId == null);
        _notifications.Published.Should().Contain(n => n.Type == NotificationTypes.AccessExpired && n.UserId == _thirdParty.User.Id);

        // Idempotente.
        (await new ExpireAccessGrantsCommandHandler(_db, _notifications).Handle(new ExpireAccessGrantsCommand(), CancellationToken.None))
            .Value.Should().Be(0);
    }

    [Fact]
    public async Task ReducingTheOriginPermissions_ShouldClampDerivedGrantsAndRevokeDependentWidenings()
    {
        var origin = await BuildChainAsync();

        var result = await new UpdateAccessGrantCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db), _notifications)
            .Handle(new UpdateAccessGrantCommand(origin.Id, ActionCodes: [ShipmentActionCodes.GrantAccess]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var derived = _db.AccessGrantList.Single(g => g.ParentGrantId == origin.Id);
        ActionCodeList.Parse(derived.ActionCodes).Should().NotContain(ShipmentActionCodes.PayFreight);
        ActionCodeList.Parse(derived.CeilingActionCodes).Should().NotContain(ShipmentActionCodes.PayFreight);
        _db.VisibilityWideningList.Single().Status.Should().Be(VisibilityWideningStatus.Revoked);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.GrantPermissionsChanged && e.AccessGrantId == derived.Id);
    }

    [Fact]
    public async Task ShorteningTheOriginValidity_ShouldShortenDerivedGrants()
    {
        var origin = await BuildChainAsync();
        var newEnd = DateTime.UtcNow.AddDays(3);

        var result = await new UpdateAccessGrantCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db), _notifications)
            .Handle(new UpdateAccessGrantCommand(origin.Id, ValidityType: AccessValidityTypes.UntilDate, ValidTo: newEnd),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _db.AccessGrantList.Single(g => g.ParentGrantId == origin.Id).ValidTo.Should().Be(newEnd);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.GrantValidityChanged && e.AccessGrantId == origin.Id);
        _notifications.Published.Should().Contain(n => n.Type == NotificationTypes.AccessUpdated && n.UserId == _thirdParty.User.Id);
    }

    [Fact]
    public async Task Widening_ShouldNotExceedTheGrantorNorTargetNonOnGrantData()
    {
        var handler = new CreateVisibilityWideningsCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db));

        // Recargos mandatorios: O para el Shipper, no hay nada que ampliar.
        var notWidenable = await handler.Handle(
            new CreateVisibilityWideningsCommand(_bl.BLNumber, ShipmentRoleCodes.Shipper, [ShipmentActionCodes.PayMandatoryLocalCharges]),
            CancellationToken.None);
        notWidenable.Error.Code.Should().Be("VisibilityWidening.NotWidenable");

        // El Shipper no posee el flete: no puede ampliarlo al Consignee.
        var shipperHandler = new CreateVisibilityWideningsCommandHandler(_db, _shipper.CurrentUser, _shipper.Evaluator(_db));
        var exceeds = await shipperHandler.Handle(
            new CreateVisibilityWideningsCommand(_bl.BLNumber, ShipmentRoleCodes.Customer, [ShipmentActionCodes.PayFreight]),
            CancellationToken.None);
        exceeds.Error.Code.Should().Be("AccessGrant.ExceedsGrantorLevel");
    }

    [Fact]
    public async Task RevokingAWidening_ShouldBeRecordedAndReversible()
    {
        var created = await new CreateVisibilityWideningsCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db))
            .Handle(new CreateVisibilityWideningsCommand(_bl.BLNumber, ShipmentRoleCodes.Shipper, [ShipmentActionCodes.PayFreight]),
                CancellationToken.None);
        var widening = created.Value.Single();
        widening.OriginGrantId.Should().BeNull();

        var revoked = await new RevokeVisibilityWideningCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db))
            .Handle(new RevokeVisibilityWideningCommand(_bl.BLNumber, widening.Id), CancellationToken.None);

        revoked.IsSuccess.Should().BeTrue();
        _db.AccessAuditEntryList.Select(e => e.EventType).Should().Contain(
            [AccessAuditEvents.WideningCreated, AccessAuditEvents.WideningRevoked]);
    }
}
