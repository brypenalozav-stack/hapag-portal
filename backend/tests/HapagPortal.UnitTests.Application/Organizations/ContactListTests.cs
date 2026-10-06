namespace HapagPortal.UnitTests.Application.Organizations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.ContactLists;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Listas de distribución de contactos (M1-06): el cliente ve y actualiza los correos por tipo de reporte; el cambio se
/// propaga al registro de contactos (P0060) por <see cref="IContactListProvider"/> y queda registrado. Los transportistas
/// no pueden (M1-11) y un perfil de consulta solo ve.
/// </summary>
public sealed class ContactListTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly IContactListProvider _provider = Substitute.For<IContactListProvider>();
    private readonly ThirdPartyTestData.Actor _customer;

    public ContactListTests()
    {
        _customer = ThirdPartyTestData.Organization(_db);
        _customer.Organization.MatchCode = "MC1";
        _provider.GetListsAsync("MC1", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ContactDistributionList>>.Success(
                [new ContactDistributionList(ContactReportTypes.Invoices, ["old@org.cl"], DateTime.UtcNow, "P0060")]));
        _provider.UpdateListAsync("MC1", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ContactListUpdateResult>.Success(new ContactListUpdateResult(
                new ContactDistributionList(call.ArgAt<string>(2), call.ArgAt<IReadOnlyList<string>>(3), DateTime.UtcNow, call.ArgAt<string>(4)),
                "P0060-REF")));
    }

    private UpdateContactListCommandHandler Update(ThirdPartyTestData.Actor actor) =>
        new(_db, actor.CurrentUser, actor.Evaluator(_db), _provider);

    [Fact]
    public async Task Get_ShouldListEveryReportType_WithTheSourceEmails()
    {
        var result = await new GetContactListsQueryHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db), _provider)
            .Handle(new GetContactListsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Available.Should().BeTrue();
        result.Value.CanEdit.Should().BeTrue();
        result.Value.Lists.Select(l => l.ReportType).Should().Equal(ContactReportTypes.All);
        result.Value.Lists.Single(l => l.ReportType == ContactReportTypes.Invoices).Emails.Should().Equal("old@org.cl");
        result.Value.Lists.Single(l => l.ReportType == ContactReportTypes.ArrivalNotice).Emails.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ShouldPropagateThroughThePort_AndRecordTheChange()
    {
        var result = await Update(_customer).Handle(
            new UpdateContactListCommand("invoices", ["Nuevo@Org.cl", "nuevo@org.cl", "pagos@org.cl"]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Emails.Should().Equal("nuevo@org.cl", "pagos@org.cl");
        await _provider.Received(1).UpdateListAsync("MC1", _customer.Organization.Country, ContactReportTypes.Invoices,
            Arg.Is<IReadOnlyList<string>>(e => e.SequenceEqual(new[] { "nuevo@org.cl", "pagos@org.cl" })),
            _customer.User.Email, Arg.Is<string>(k => k.StartsWith("contacts-")), Arg.Any<CancellationToken>());

        var change = _db.ContactListChangeList.Should().ContainSingle().Subject;
        change.Status.Should().Be(ContactListChangeStatus.Propagated);
        change.PreviousEmails.Should().Be("old@org.cl");
        change.NewEmails.Should().Be("nuevo@org.cl;pagos@org.cl");
        change.SourceReference.Should().Be("P0060-REF");
        change.ChangedByUserId.Should().Be(_customer.User.Id);

        var history = await new GetContactListHistoryQueryHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db))
            .Handle(new GetContactListHistoryQuery(), CancellationToken.None);
        history.Value.Should().ContainSingle().Which.NewEmails.Should().Equal("nuevo@org.cl", "pagos@org.cl");
    }

    [Fact]
    public async Task Update_WhenTheSourceFails_ShouldRecordTheFailedAttemptAndReturnTheError()
    {
        _provider.UpdateListAsync("MC1", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ContactListUpdateResult>.Failure(DomainErrors.Integration.Unavailable("Contacts")));

        var result = await Update(_customer).Handle(new UpdateContactListCommand(ContactReportTypes.FreeTime, ["ops@org.cl"]), CancellationToken.None);

        result.Error.Code.Should().Be("Integration.Unavailable");
        var change = _db.ContactListChangeList.Should().ContainSingle().Subject;
        change.Status.Should().Be(ContactListChangeStatus.Failed);
        change.ErrorCode.Should().Be("Integration.Unavailable");
    }

    [Fact]
    public async Task CarrierOrViewer_ShouldNotUpdate()
    {
        var carrier = ThirdPartyTestData.Organization(_db, OrganizationTypes.Carrier);
        carrier.Organization.MatchCode = "MC2";
        var viewer = AccessTestData.AddMember(_db, _customer.Organization, profile: RoleCodes.OrgViewer);
        var viewerActor = new ThirdPartyTestData.Actor(_customer.Organization, viewer, AccessTestData.CurrentUser(viewer));

        var byCarrier = await Update(carrier).Handle(new UpdateContactListCommand(ContactReportTypes.Invoices, ["a@b.cl"]), CancellationToken.None);
        var byViewer = await Update(viewerActor).Handle(new UpdateContactListCommand(ContactReportTypes.Invoices, ["a@b.cl"]), CancellationToken.None);
        var viewerRead = await new GetContactListsQueryHandler(_db, viewerActor.CurrentUser, viewerActor.Evaluator(_db), _provider)
            .Handle(new GetContactListsQuery(), CancellationToken.None);

        byCarrier.Error.Code.Should().Be("ContactList.NotAllowed");
        byViewer.Error.Should().Be(Error.Forbidden);
        viewerRead.IsSuccess.Should().BeTrue();
        viewerRead.Value.CanEdit.Should().BeFalse();
        _db.ContactListChangeList.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_UnknownReportTypeOrMissingMatchCode_ShouldFail()
    {
        var unknown = await Update(_customer).Handle(new UpdateContactListCommand("WEEKLY", ["a@b.cl"]), CancellationToken.None);
        _customer.Organization.MatchCode = null;
        var noMatchCode = await Update(_customer).Handle(new UpdateContactListCommand(ContactReportTypes.Invoices, ["a@b.cl"]), CancellationToken.None);

        unknown.Error.Code.Should().Be("ContactList.UnknownReportType");
        noMatchCode.Error.Code.Should().Be("ContactList.MatchCodeRequired");
        new UpdateContactListCommandValidator().Validate(new UpdateContactListCommand("INVOICES", ["no-es-correo"])).IsValid.Should().BeFalse();
    }
}
