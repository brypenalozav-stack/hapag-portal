namespace HapagPortal.UnitTests.Application.Announcements;

using FluentAssertions;
using HapagPortal.Application.Announcements;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Comunicados masivos (M1-26): segmentación por país y operación, vigencia, publicación y retiro desde el área interna
/// con registro de cambios (NF-15) y aviso opcional en la bandeja al segmento (M1-25).
/// </summary>
public sealed class AnnouncementTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly FakeNotificationPublisher _notifications = new();
    private readonly ICurrentUserService _admin = Substitute.For<ICurrentUserService>();

    public AnnouncementTests()
    {
        _admin.UserId.Returns(Guid.NewGuid());
        _admin.Email.Returns("admin@hapag-lloyd.cl");
    }

    private AnnouncementPublisher Publisher() => new(_db, _admin, _notifications);

    private static CreateAnnouncementCommand Command(
        string[] countries,
        string operation,
        bool notify = false,
        bool publish = true,
        DateTime? validFrom = null,
        DateTime? validTo = null) =>
        new("Título", "Title", "Cuerpo", "Body", countries, operation, AnnouncementSeverities.Info, validFrom, validTo, notify, publish);

    private static ICurrentUserService Client(string country)
    {
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(Guid.NewGuid());
        user.Country.Returns(country);
        return user;
    }

    private async Task<AnnouncementDto> CreateAsync(CreateAnnouncementCommand command)
    {
        var result = await new CreateAnnouncementCommandHandler(_db, _admin, Publisher()).Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        return result.Value;
    }

    private async Task<IReadOnlyList<PublishedAnnouncementDto>> CurrentAsync(string country, string? operation = null) =>
        (await new GetCurrentAnnouncementsQueryHandler(_db, Client(country))
            .Handle(new GetCurrentAnnouncementsQuery(null, operation), CancellationToken.None)).Value;

    [Fact]
    public async Task Current_ShouldSegmentByCountryAndOperation_WithPublicationDate()
    {
        var clImport = await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Import));
        var boExport = await CreateAsync(Command([CountryCodes.Bolivia], AnnouncementOperations.Export));
        var both = await CreateAsync(Command([CountryCodes.Chile, CountryCodes.Bolivia], AnnouncementOperations.Both));

        var chile = await CurrentAsync(CountryCodes.Chile);
        var chileExport = await CurrentAsync(CountryCodes.Chile, AnnouncementOperations.Export);
        var bolivia = await CurrentAsync(CountryCodes.Bolivia);

        chile.Select(a => a.Id).Should().BeEquivalentTo([clImport.Id, both.Id]);
        chileExport.Select(a => a.Id).Should().BeEquivalentTo([both.Id]);
        bolivia.Select(a => a.Id).Should().BeEquivalentTo([boExport.Id, both.Id]);
        chile.Should().OnlyContain(a => a.PublishedAt != default);
    }

    [Fact]
    public async Task Current_ShouldHideDraftsUnpublishedAndOutOfValidity()
    {
        await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Both, publish: false));
        await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Both, validFrom: DateTime.UtcNow.AddDays(2)));
        var expiring = await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Both));
        _db.AnnouncementList.Single(a => a.Id == expiring.Id).ValidTo = DateTime.UtcNow.AddMinutes(-1);
        var withdrawn = await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Both));
        var visible = await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Both));

        var unpublished = await new UnpublishAnnouncementCommandHandler(_db, _admin)
            .Handle(new UnpublishAnnouncementCommand(withdrawn.Id), CancellationToken.None);

        unpublished.Value.Status.Should().Be(AnnouncementStatus.Unpublished);
        (await CurrentAsync(CountryCodes.Chile)).Select(a => a.Id).Should().BeEquivalentTo([visible.Id]);
    }

    [Fact]
    public async Task Publish_WithNotification_ShouldNotifyOnlyTheSegmentOnce()
    {
        var chilean = AccessTestData.AddOrganization(_db, country: CountryCodes.Chile);
        var chileanUser = AccessTestData.AddMember(_db, chilean);
        var bolivian = AccessTestData.AddOrganization(_db, country: CountryCodes.Bolivia);
        AccessTestData.AddMember(_db, bolivian);
        var pending = AccessTestData.AddOrganization(_db, status: OrganizationStatus.PendingValidation, country: CountryCodes.Chile);
        AccessTestData.AddMember(_db, pending);
        var internalOrg = AccessTestData.AddOrganization(_db, OrganizationTypes.Internal, country: CountryCodes.Chile);
        AccessTestData.AddMember(_db, internalOrg, profile: null);

        var draft = await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Import, notify: true, publish: false));
        _notifications.Published.Should().BeEmpty();

        var published = await new PublishAnnouncementCommandHandler(_db, Publisher())
            .Handle(new PublishAnnouncementCommand(draft.Id), CancellationToken.None);

        published.IsSuccess.Should().BeTrue();
        published.Value.IsCurrent.Should().BeTrue();
        published.Value.NotifiedAt.Should().NotBeNull();
        var notification = _notifications.Published.Should().ContainSingle().Subject;
        notification.UserId.Should().Be(chileanUser.Id);
        notification.Type.Should().Be(NotificationTypes.AnnouncementPublished);
        notification.Action.Should().Be(new NotificationAction(NotificationActionTypes.OpenAnnouncement, draft.Id.ToString()));

        // Retirar y volver a publicar no repite el aviso.
        await new UnpublishAnnouncementCommandHandler(_db, _admin).Handle(new UnpublishAnnouncementCommand(draft.Id), CancellationToken.None);
        await new PublishAnnouncementCommandHandler(_db, Publisher()).Handle(new PublishAnnouncementCommand(draft.Id), CancellationToken.None);
        _notifications.Published.Should().ContainSingle();
    }

    [Fact]
    public async Task Publish_ExpiredOrAlreadyPublished_ShouldFail()
    {
        var expired = await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Both, publish: false,
            validFrom: DateTime.UtcNow.AddDays(-5), validTo: DateTime.UtcNow.AddDays(-1)));
        var published = await CreateAsync(Command([CountryCodes.Chile], AnnouncementOperations.Both));

        var first = await new PublishAnnouncementCommandHandler(_db, Publisher()).Handle(new PublishAnnouncementCommand(expired.Id), CancellationToken.None);
        var second = await new PublishAnnouncementCommandHandler(_db, Publisher()).Handle(new PublishAnnouncementCommand(published.Id), CancellationToken.None);

        first.Error.Code.Should().Be("Announcement.Expired");
        second.Error.Code.Should().Be("Announcement.InvalidTransition");
    }

    [Fact]
    public async Task Changes_ShouldBeLoggedForNf15()
    {
        var created = await CreateAsync(Command([CountryCodes.Bolivia], AnnouncementOperations.Export, publish: false));

        await new UpdateAnnouncementCommandHandler(_db, _admin).Handle(new UpdateAnnouncementCommand(
            created.Id, "Nuevo", "New", "Cuerpo", "Body", [CountryCodes.Bolivia, CountryCodes.Chile], AnnouncementOperations.Both,
            AnnouncementSeverities.Important, null, null), CancellationToken.None);
        await new DeleteAnnouncementCommandHandler(_db, _admin).Handle(new DeleteAnnouncementCommand(created.Id), CancellationToken.None);

        var history = await new GetAnnouncementHistoryQueryHandler(_db).Handle(new GetAnnouncementHistoryQuery(created.Id), CancellationToken.None);

        history.Value.Select(h => h.Action).Should().BeEquivalentTo(
            [MaintainerActions.Created, MaintainerActions.Updated, MaintainerActions.Deactivated]);
        history.Value.Single(h => h.Action == MaintainerActions.Updated).Current!.Countries.Should().Be("CL,BO");
        history.Value.Should().OnlyContain(h => h.ChangedBy == "admin@hapag-lloyd.cl");
        _db.AnnouncementList.Single().DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Validator_ShouldRequireSegmentAndBothLanguages()
    {
        var validator = new CreateAnnouncementCommandValidator();

        validator.Validate(Command([], AnnouncementOperations.Import)).IsValid.Should().BeFalse();
        validator.Validate(Command(["AR"], AnnouncementOperations.Import)).IsValid.Should().BeFalse();
        validator.Validate(Command([CountryCodes.Chile], "Transit")).IsValid.Should().BeFalse();
        validator.Validate(Command([CountryCodes.Chile], AnnouncementOperations.Import) with { TitleEn = "" }).IsValid.Should().BeFalse();
        validator.Validate(Command([CountryCodes.Chile], AnnouncementOperations.Import)).IsValid.Should().BeTrue();
    }
}
