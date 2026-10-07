namespace HapagPortal.UnitTests.Application.ThirdPartyAccess;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.ParentCompany;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Visibilidad hacia la empresa matriz (M1-21): la filial pide el vínculo, Hapag-Lloyd lo aprueba, la filial decide si
/// comparte sus BL; la matriz los ve sin asignación BL por BL, solo para consulta, identificando la filial de origen y
/// sin mezclar la información de otras organizaciones.
/// </summary>
public sealed class ParentCompanyVisibilityTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly FakeNotificationPublisher _notifications = new();
    private readonly ThirdPartyTestData.Actor _subsidiary;
    private readonly ThirdPartyTestData.Actor _parent;
    private readonly ThirdPartyTestData.Actor _stranger;
    private readonly BillOfLading _owned;
    private readonly BillOfLading _consigned;
    private readonly BillOfLading _strangers;
    private readonly ICurrentUserService _hapag;

    public ParentCompanyVisibilityTests()
    {
        _subsidiary = ThirdPartyTestData.Organization(_db, name: "Filial");
        _parent = ThirdPartyTestData.Organization(_db, name: "Holding");
        _stranger = ThirdPartyTestData.Organization(_db, name: "Otra");
        _owned = AccessTestData.AddBl(_db, _subsidiary.Organization.Id, "BL-OWNED");
        _consigned = AccessTestData.AddBl(_db, _stranger.Organization.Id, "BL-CONSIGNED");
        AccessTestData.AddRole(_db, _consigned, _subsidiary.Organization, ShipmentRoleCodes.Consignee);
        _strangers = AccessTestData.AddBl(_db, _stranger.Organization.Id, "BL-STRANGER");

        _hapag = Substitute.For<ICurrentUserService>();
        _hapag.UserId.Returns(Guid.NewGuid());
        _hapag.Email.Returns("admin@hapag-lloyd.cl");
    }

    private async Task<IReadOnlyList<string>> VisibleTo(ThirdPartyTestData.Actor actor)
    {
        var evaluator = actor.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();
        return evaluator.FilterAccessible(_db.BillsOfLading, scope).Select(b => b.BLNumber).ToList();
    }

    private async Task<ParentLinkDto> RequestAndApproveAsync(bool visibility = true)
    {
        var requested = await new RequestParentLinkCommandHandler(_db, _subsidiary.CurrentUser, _subsidiary.Evaluator(_db), _notifications)
            .Handle(new RequestParentLinkCommand(_parent.Organization.Id, visibility), CancellationToken.None);
        requested.IsSuccess.Should().BeTrue(requested.IsFailure ? requested.Error.Message : string.Empty);

        var approved = await new ApproveParentLinkCommandHandler(_db, _hapag, _notifications)
            .Handle(new ApproveParentLinkCommand(requested.Value.Id), CancellationToken.None);
        approved.IsSuccess.Should().BeTrue();
        return approved.Value;
    }

    [Fact]
    public async Task Request_ShouldStayPending_UntilHapagLloydApproves()
    {
        var requested = await new RequestParentLinkCommandHandler(_db, _subsidiary.CurrentUser, _subsidiary.Evaluator(_db), _notifications)
            .Handle(new RequestParentLinkCommand(_parent.Organization.Id), CancellationToken.None);

        requested.Value.Status.Should().Be(ParentLinkStatus.Pending);
        (await VisibleTo(_parent)).Should().BeEmpty();
        var review = _notifications.Published.Should().ContainSingle().Subject;
        review.RoleCode.Should().Be(RoleCodes.Administrador);
        review.Action.Should().Be(new NotificationAction(NotificationActionTypes.ReviewParentLink, requested.Value.Id.ToString()));

        await new ApproveParentLinkCommandHandler(_db, _hapag, _notifications)
            .Handle(new ApproveParentLinkCommand(requested.Value.Id, "Grupo verificado"), CancellationToken.None);

        (await VisibleTo(_parent)).Should().BeEquivalentTo(["BL-OWNED", "BL-CONSIGNED"]);
        _notifications.Published.Count(n => n.Type == NotificationTypes.ParentLinkApproved).Should().Be(2);   // filial y matriz
        _db.AccessAuditEntryList.Select(e => e.EventType).Should().Contain(
            [AccessAuditEvents.ParentLinkRequested, AccessAuditEvents.ParentLinkApproved]);
    }

    [Fact]
    public async Task Parent_ShouldSeeTheSubsidiaryShipments_ViewOnly_WithTheirOrigin()
    {
        await RequestAndApproveAsync();
        var evaluator = _parent.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();

        var permissions = await evaluator.EvaluateAsync(scope, _owned);
        var sources = await evaluator.GetAccessSourcesAsync(scope, [_owned, _consigned]);

        permissions.AccessSource.Should().Be(ShipmentAccessSources.Parent);
        permissions.OriginOrganizationId.Should().Be(_subsidiary.Organization.Id);
        permissions.CanOperate.Should().BeFalse();
        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.ViewTracking).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
        permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges).Should().BeFalse();
        permissions.Can(ShipmentActionCodes.GrantAccess).Should().BeFalse();
        sources.Values.Should().OnlyContain(s => s == ShipmentAccessSources.Parent);

        var search = await new SearchShipmentsQueryHandler(_db, evaluator).Handle(new SearchShipmentsQuery(), CancellationToken.None);
        search.Value.Items.Should().HaveCount(2)
            .And.OnlyContain(i => i.AccessSource == ShipmentAccessSources.Parent && i.OriginOrganization!.Id == _subsidiary.Organization.Id);
    }

    [Fact]
    public async Task Segregation_ShouldHoldForOtherOrganizationsAndWithoutTransitivity()
    {
        await RequestAndApproveAsync();
        // La matriz de la matriz no ve los BL de la filial (no es transitivo).
        var grandParent = ThirdPartyTestData.Organization(_db, name: "Abuela");
        _db.OrganizationParentLinkList.Add(new OrganizationParentLink
        {
            OrganizationId = _parent.Organization.Id,
            ParentOrganizationId = grandParent.Organization.Id,
            Status = ParentLinkStatus.Active,
            VisibilityEnabled = true,
            RequestedBy = "seed"
        });

        (await VisibleTo(grandParent)).Should().BeEmpty();
        (await VisibleTo(_stranger)).Should().BeEquivalentTo(["BL-CONSIGNED", "BL-STRANGER"]);
        (await VisibleTo(_parent)).Should().NotContain("BL-STRANGER");

        // El listado de la matriz no puede filtrarse por una organización que no es su filial.
        var evaluator = _parent.Evaluator(_db);
        var foreign = await new SearchShipmentsQueryHandler(_db, evaluator)
            .Handle(new SearchShipmentsQuery(OrganizationId: _stranger.Organization.Id), CancellationToken.None);
        var bySubsidiary = await new SearchShipmentsQueryHandler(_db, _parent.Evaluator(_db))
            .Handle(new SearchShipmentsQuery(OrganizationId: _subsidiary.Organization.Id), CancellationToken.None);

        foreign.Error.Should().Be(Error.Forbidden);
        bySubsidiary.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Subsidiary_ShouldToggleTheVisibility_AndRemoveTheLink()
    {
        await RequestAndApproveAsync();
        var setVisibility = new SetParentVisibilityCommandHandler(_db, _subsidiary.CurrentUser, _subsidiary.Evaluator(_db), _notifications);

        var off = await setVisibility.Handle(new SetParentVisibilityCommand(false), CancellationToken.None);
        (await VisibleTo(_parent)).Should().BeEmpty();
        var on = await setVisibility.Handle(new SetParentVisibilityCommand(true), CancellationToken.None);
        (await VisibleTo(_parent)).Should().HaveCount(2);

        off.Value.VisibilityEnabled.Should().BeFalse();
        on.Value.VisibilityEnabled.Should().BeTrue();
        _notifications.Published.Count(n => n.Type == NotificationTypes.ParentVisibilityChanged).Should().Be(2);
        _db.AccessAuditEntryList.Select(e => e.EventType).Should().Contain(
            [AccessAuditEvents.ParentVisibilityDisabled, AccessAuditEvents.ParentVisibilityEnabled]);

        var removed = await new RemoveParentLinkCommandHandler(_db, _subsidiary.CurrentUser, _subsidiary.Evaluator(_db))
            .Handle(new RemoveParentLinkCommand(), CancellationToken.None);
        removed.IsSuccess.Should().BeTrue();
        (await VisibleTo(_parent)).Should().BeEmpty();
    }

    [Fact]
    public async Task Request_InvalidCases_ShouldFail()
    {
        var agency = ThirdPartyTestData.Organization(_db, OrganizationTypes.CustomsAgency);
        var handler = new RequestParentLinkCommandHandler(_db, _subsidiary.CurrentUser, _subsidiary.Evaluator(_db), _notifications);

        var self = await handler.Handle(new RequestParentLinkCommand(_subsidiary.Organization.Id), CancellationToken.None);
        var agencyParent = await handler.Handle(new RequestParentLinkCommand(agency.Organization.Id), CancellationToken.None);
        var byAgency = await new RequestParentLinkCommandHandler(_db, agency.CurrentUser, agency.Evaluator(_db), _notifications)
            .Handle(new RequestParentLinkCommand(_parent.Organization.Id), CancellationToken.None);
        await handler.Handle(new RequestParentLinkCommand(_parent.Organization.Id), CancellationToken.None);
        var duplicate = await handler.Handle(new RequestParentLinkCommand(_stranger.Organization.Id), CancellationToken.None);
        var cycle = await new RequestParentLinkCommandHandler(_db, _parent.CurrentUser, _parent.Evaluator(_db), _notifications)
            .Handle(new RequestParentLinkCommand(_subsidiary.Organization.Id), CancellationToken.None);

        self.Error.Code.Should().Be("ParentLink.SelfLink");
        agencyParent.Error.Code.Should().Be("ParentLink.InvalidParent");
        byAgency.Error.Code.Should().Be("ParentLink.NotAllowed");
        duplicate.Error.Code.Should().Be("ParentLink.AlreadyExists");
        cycle.Error.Code.Should().Be("ParentLink.Cycle");
    }

    [Fact]
    public async Task Reject_ShouldCloseTheRequestWithoutVisibility()
    {
        var requested = await new RequestParentLinkCommandHandler(_db, _subsidiary.CurrentUser, _subsidiary.Evaluator(_db), _notifications)
            .Handle(new RequestParentLinkCommand(_parent.Organization.Id), CancellationToken.None);

        var rejected = await new RejectParentLinkCommandHandler(_db, _hapag, _notifications)
            .Handle(new RejectParentLinkCommand(requested.Value.Id, "No acredita el grupo"), CancellationToken.None);
        var approveAfter = await new ApproveParentLinkCommandHandler(_db, _hapag, _notifications)
            .Handle(new ApproveParentLinkCommand(requested.Value.Id), CancellationToken.None);

        rejected.Value.Status.Should().Be(ParentLinkStatus.Rejected);
        rejected.Value.DecisionNotes.Should().Be("No acredita el grupo");
        approveAfter.Error.Code.Should().Be("ParentLink.NotPending");
        (await VisibleTo(_parent)).Should().BeEmpty();

        var view = await new GetParentCompanyQueryHandler(_db, _subsidiary.CurrentUser, _subsidiary.Evaluator(_db))
            .Handle(new GetParentCompanyQuery(), CancellationToken.None);
        view.Value.Link.Should().BeNull();
        view.Value.CanManage.Should().BeTrue();
    }
}
