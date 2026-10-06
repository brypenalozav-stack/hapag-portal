namespace HapagPortal.UnitTests.Application.ThirdPartyAccess;

using FluentAssertions;
using HapagPortal.Application.ThirdPartyAccess.Grants;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>Otorgamiento individual, masivo y mandato (M1-03, M1-12, M1-14, M1-15) y su registro (M1-23).</summary>
public sealed class GrantAccessCommandTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly FakeNotificationPublisher _notifications = new();
    private readonly ThirdPartyTestData.Actor _owner;
    private readonly ThirdPartyTestData.Actor _agency;

    public GrantAccessCommandTests()
    {
        _owner = ThirdPartyTestData.Organization(_db, name: "Owner");
        _agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency, name: "Agency");
    }

    private GrantAccessCommandHandler Handler(ThirdPartyTestData.Actor actor) =>
        new(_db, actor.CurrentUser, actor.Evaluator(_db), _notifications);

    private BillOfLading OwnedBl(string number, string role = ShipmentRoleCodes.Consignee)
    {
        var bl = AccessTestData.AddBl(_db, Guid.NewGuid(), number, bookingNumber: $"BKG-{number}");
        AccessTestData.AddRole(_db, bl, _owner.Organization, role);
        return bl;
    }

    [Fact]
    public async Task Individual_ShouldCreateActiveGrant_AuditIt_AndNotifyTheGrantee()
    {
        var bl = OwnedBl("BL-1");

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id,
            BlNumbers: [bl.BLNumber],
            ValidityType: AccessValidityTypes.Duration,
            DurationDays: 30,
            ActionCodes: [ShipmentActionCodes.PayImportDemurrage]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Updated.Should().Be(1);
        var grant = _db.AccessGrantList.Should().ContainSingle().Subject;
        grant.Status.Should().Be(AccessGrantStatus.Active);
        grant.GrantType.Should().Be(AccessGrantTypes.Individual);
        grant.GrantorRole.Should().Be(ShipmentRoleCodes.Consignee);
        grant.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
        ActionCodeList.Parse(grant.ActionCodes).Should().BeEquivalentTo(
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayImportDemurrage]);   // la vista siempre se incluye

        _db.AccessAuditEntryList.Should().ContainSingle(e =>
            e.EventType == AccessAuditEvents.GrantCreated
            && e.AccessGrantId == grant.Id
            && e.ActorUserId == _owner.User.Id
            && e.BlNumber == bl.BLNumber);
        _notifications.Published.Should().ContainSingle(n =>
            n.Type == NotificationTypes.AccessGranted && n.UserId == _agency.User.Id);

        var dto = result.Value.Grants.Single();
        dto.EffectiveActionCodes.Should().Contain(ShipmentActionCodes.PayImportDemurrage);
        dto.Direction.Should().Be("Given");
    }

    [Fact]
    public async Task GrantingMoreThanTheGrantorHolds_ShouldFail()
    {
        // Shipper: flete es X (o) para él, no lo posee y no puede otorgarlo (M1-11).
        var bl = OwnedBl("BL-SH", ShipmentRoleCodes.Shipper);

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id, BlNumbers: [bl.BLNumber], ActionCodes: [ShipmentActionCodes.PayFreight]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AccessGrant.ExceedsGrantorLevel");
        _db.AccessGrantList.Should().BeEmpty();
    }

    [Fact]
    public async Task GrantingWhatTheRecipientCannotReceive_ShouldFail()
    {
        var bl = OwnedBl("BL-X");

        // Estado de cuenta: X para la agencia de aduanas.
        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id, BlNumbers: [bl.BLNumber], ActionCodes: [ShipmentActionCodes.ViewAccountStatement]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AccessGrant.NotGrantable");
    }

    [Fact]
    public async Task Bulk_ShouldReportUpdatedCount_AndSkipWhatIsNotAccessible()
    {
        var first = OwnedBl("BL-A");
        var second = OwnedBl("BL-B");
        var foreign = AccessTestData.AddBl(_db, Guid.NewGuid(), "BL-FOREIGN");

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id,
            BlNumbers: [first.BLNumber, second.BLNumber, foreign.BLNumber]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Requested.Should().Be(3);
        result.Value.Updated.Should().Be(2);
        result.Value.Skipped.Should().ContainSingle(s => s.Reference == foreign.BLNumber && s.Code == "BillOfLading.NotFound");
        _db.AccessGrantList.Should().HaveCount(2).And.OnlyContain(g => g.GrantType == AccessGrantTypes.Bulk);
    }

    [Fact]
    public async Task ByBookingNumber_ShouldResolveTheBl()
    {
        var bl = OwnedBl("BL-BK");

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id, BookingNumbers: [bl.BookingNumber!]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _db.AccessGrantList.Should().ContainSingle(g => g.BillOfLadingId == bl.Id);
    }

    [Fact]
    public async Task GrantingAgain_ShouldUpdateTheOpenGrant_NotDuplicateIt()
    {
        var bl = OwnedBl("BL-AGAIN");
        await Handler(_owner).Handle(new GrantAccessCommand(_agency.Organization.Id, BlNumbers: [bl.BLNumber]), CancellationToken.None);

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id, BlNumbers: [bl.BLNumber], ActionCodes: [ShipmentActionCodes.ViewTracking]), CancellationToken.None);

        result.Value.Created.Should().Be(0);
        result.Value.Modified.Should().Be(1);
        _db.AccessGrantList.Should().ContainSingle();
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.GrantPermissionsChanged);
    }

    [Fact]
    public async Task SelfGrant_ShouldFail()
    {
        var bl = OwnedBl("BL-SELF");

        var result = await Handler(_owner).Handle(
            new GrantAccessCommand(_owner.Organization.Id, BlNumbers: [bl.BLNumber]), CancellationToken.None);

        result.Error.Should().Be(HapagPortal.Domain.Errors.DomainErrors.AccessGrant.SelfGrant);
    }

    [Fact]
    public async Task ViewerProfile_ShouldNotGrant()
    {
        var viewer = ThirdPartyTestData.Organization(_db, profile: RoleCodes.OrgViewer);
        var bl = AccessTestData.AddBl(_db, viewer.Organization.Id, "BL-VIEW");

        var result = await Handler(viewer).Handle(
            new GrantAccessCommand(_agency.Organization.Id, BlNumbers: [bl.BLNumber]), CancellationToken.None);

        result.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task Mandate_WithoutTermsAcceptance_ShouldStayPending_UntilAccepted()
    {
        var bl = OwnedBl("BL-MANDATE");

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id,
            BlNumbers: [bl.BLNumber],
            ValidityType: AccessValidityTypes.UntilDate,
            ValidTo: DateTime.UtcNow.AddMonths(6),
            ActionCodes: [ShipmentActionCodes.PayFreight],
            IsMandate: true), CancellationToken.None);

        var grant = _db.AccessGrantList.Single();
        grant.Status.Should().Be(AccessGrantStatus.PendingAcceptance);

        // Pendiente: no habilita nada al mandatario.
        var agencyEvaluator = _agency.Evaluator(_db);
        (await agencyEvaluator.EvaluateAsync(await agencyEvaluator.GetScopeAsync(), bl)).HasAccess.Should().BeFalse();

        var accept = new AcceptMandateTermsCommandHandler(_db, _owner.CurrentUser, _owner.Evaluator(_db), _notifications);
        var accepted = await accept.Handle(new AcceptMandateTermsCommand(grant.Id, MandateTerms.CurrentVersion), CancellationToken.None);

        accepted.IsSuccess.Should().BeTrue();
        grant.Status.Should().Be(AccessGrantStatus.Active);
        grant.TermsVersion.Should().Be(MandateTerms.CurrentVersion);
        grant.TermsAcceptedByUserId.Should().Be(_owner.User.Id);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.MandateTermsAccepted);

        var later = _agency.Evaluator(_db);
        var permissions = await later.EvaluateAsync(await later.GetScopeAsync(), bl);
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeTrue();
        permissions.GrantFor(ShipmentActionCodes.PayFreight)!.IsMandate.Should().BeTrue();
        result.Value.Grants.Single().IsMandate.Should().BeTrue();
    }

    [Fact]
    public async Task Mandate_WithWrongTermsVersion_ShouldFail()
    {
        var bl = OwnedBl("BL-MV");

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id,
            BlNumbers: [bl.BLNumber],
            ValidityType: AccessValidityTypes.Duration,
            DurationDays: 180,
            ActionCodes: [ShipmentActionCodes.PayFreight],
            IsMandate: true,
            AcceptTerms: true,
            TermsVersion: "OLD"), CancellationToken.None);

        result.Error.Code.Should().Be("AccessGrant.TermsVersionMismatch");
    }

    [Fact]
    public void Validator_ShouldRequireScopeAndValidityForMandates()
    {
        var validator = new GrantAccessCommandValidator();

        var result = validator.Validate(new GrantAccessCommand(Guid.NewGuid(), BlNumbers: ["BL"], IsMandate: true));

        result.Errors.Select(e => e.PropertyName).Should().Contain(["ActionCodes", "ValidityType"]);
    }

    [Fact]
    public void Validator_ShouldRequireReferences()
    {
        var result = new GrantAccessCommandValidator().Validate(new GrantAccessCommand(Guid.NewGuid()));

        result.Errors.Should().Contain(e => e.PropertyName == "References");
    }

    [Fact]
    public async Task UntilDateInThePast_ShouldFail()
    {
        var bl = OwnedBl("BL-PAST");

        var result = await Handler(_owner).Handle(new GrantAccessCommand(
            _agency.Organization.Id,
            BlNumbers: [bl.BLNumber],
            ValidityType: AccessValidityTypes.UntilDate,
            ValidTo: DateTime.UtcNow.AddDays(-1)), CancellationToken.None);

        result.Error.Code.Should().Be("AccessGrant.InvalidValidity");
    }
}
