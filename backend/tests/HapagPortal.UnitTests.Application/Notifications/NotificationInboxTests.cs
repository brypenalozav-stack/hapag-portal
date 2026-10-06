namespace HapagPortal.UnitTests.Application.Notifications;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.RequestMembership;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Application.Notifications.GetMy;
using HapagPortal.Application.Notifications.MarkAllRead;
using HapagPortal.Application.Notifications.MarkRead;
using HapagPortal.Application.Notifications.Preferences;
using HapagPortal.Application.Notifications.UnreadCount;
using HapagPortal.Application.Organizations.JoinRequests;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Bandeja de notificaciones (M1-25): filtros por tipo y módulo, referencia al embarque o la gestión, acciones que se
/// resuelven al decidir la gestión, marca de lectura propia y preferencias de correo por tipo.
/// </summary>
public sealed class NotificationInboxTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public NotificationInboxTests()
    {
        _currentUser.UserId.Returns(_userId);
        _currentUser.Roles.Returns([RoleCodes.Supervisor]);
    }

    private Notification Add(string type, Guid? userId = null, string? role = null, string? module = null, string? blNumber = null,
        string? actionType = null, string? actionTarget = null, DateTime? createdAt = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            RoleCode = role,
            Type = type,
            Title = type,
            Body = type,
            Module = module,
            BlNumber = blNumber,
            EntityType = blNumber is null ? null : NotificationEntityTypes.Shipment,
            EntityId = blNumber,
            ActionType = actionType,
            ActionTargetId = actionTarget,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        _db.NotificationList.Add(notification);
        return notification;
    }

    [Fact]
    public async Task Inbox_ShouldFilterByModuleTypeAndBl_AndExposeLinkAndAction()
    {
        Add(NotificationTypes.PaymentConfirmed, _userId, module: NotificationModules.Payments, blNumber: "BL-1",
            actionType: NotificationActionTypes.OpenPayment, actionTarget: "pay-1", createdAt: DateTime.UtcNow.AddMinutes(-1));
        // Anterior a la Ola I: sin módulo guardado, se deduce del tipo.
        Add(NotificationTypes.AccessGranted, _userId, createdAt: DateTime.UtcNow.AddMinutes(-2));
        Add(NotificationTypes.DeadlineOverdue, role: RoleCodes.Supervisor, module: NotificationModules.Deadlines);
        Add(NotificationTypes.PaymentConfirmed, Guid.NewGuid(), module: NotificationModules.Payments);   // de otro usuario

        var handler = new GetMyNotificationsQueryHandler(_db, _currentUser);

        var all = await handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);
        var payments = await handler.Handle(new GetMyNotificationsQuery(Module: NotificationModules.Payments), CancellationToken.None);
        var access = await handler.Handle(new GetMyNotificationsQuery(Module: NotificationModules.Access), CancellationToken.None);
        var byBl = await handler.Handle(new GetMyNotificationsQuery(BlNumber: "BL-1"), CancellationToken.None);
        var byType = await handler.Handle(new GetMyNotificationsQuery(Type: NotificationTypes.DeadlineOverdue), CancellationToken.None);

        all.Value.Should().HaveCount(3);
        payments.Value.Should().ContainSingle();
        var payment = payments.Value[0];
        payment.Link.Should().BeEquivalentTo(new NotificationLinkDto(NotificationEntityTypes.Shipment, "BL-1", null, "BL-1"));
        payment.Action.Should().NotBeNull();
        payment.Action!.Type.Should().Be(NotificationActionTypes.OpenPayment);
        payment.Action.Available.Should().BeTrue();
        access.Value.Should().ContainSingle().Which.Module.Should().Be(NotificationModules.Access);
        byBl.Value.Should().ContainSingle();
        byType.Value.Should().ContainSingle().Which.Type.Should().Be(NotificationTypes.DeadlineOverdue);
    }

    [Fact]
    public async Task UnreadSummary_ShouldCountByModuleAndActionable()
    {
        Add(NotificationTypes.JoinRequestReceived, _userId, NotificationModules.Organization,
            actionType: NotificationActionTypes.ApproveJoinRequest, actionTarget: "u-1");
        Add(NotificationTypes.PaymentConfirmed, _userId, module: NotificationModules.Payments);
        Add(NotificationTypes.PaymentConfirmed, _userId, module: NotificationModules.Payments).ReadAt = DateTime.UtcNow;

        var result = await new GetUnreadSummaryQueryHandler(_db, _currentUser).Handle(new GetUnreadSummaryQuery(), CancellationToken.None);

        result.Value.Count.Should().Be(2);
        result.Value.ByModule.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            [NotificationModules.Organization] = 1,
            [NotificationModules.Payments] = 1
        });
        result.Value.Actionable.Should().Be(1);
    }

    [Fact]
    public async Task MarkRead_OfAnotherUsersNotification_ShouldReturnNotFound()
    {
        var foreign = Add(NotificationTypes.PaymentConfirmed, Guid.NewGuid());

        var result = await new MarkNotificationReadCommandHandler(_db, _currentUser)
            .Handle(new MarkNotificationReadCommand(foreign.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.NotFound");
        foreign.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkAllRead_ByModule_ShouldOnlyMarkThatModule()
    {
        var payment = Add(NotificationTypes.PaymentConfirmed, _userId, NotificationModules.Payments);
        var access = Add(NotificationTypes.AccessGranted, _userId, NotificationModules.Access);

        var result = await new MarkAllNotificationsReadCommandHandler(_db, _currentUser)
            .Handle(new MarkAllNotificationsReadCommand(NotificationModules.Payments), CancellationToken.None);

        result.Value.Should().Be(1);
        payment.ReadAt.Should().NotBeNull();
        access.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task JoinRequest_ShouldBeActionable_AndTheActionResolvesWhenApproved()
    {
        var organization = AccessTestData.AddOrganization(_db);
        var admin = AccessTestData.AddMember(_db, organization);
        var publisher = new FakeNotificationPublisher();
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash(Arg.Any<string>()).Returns("hash");

        var requested = await new RequestOrganizationMembershipCommandHandler(_db, hasher, publisher).Handle(
            new RequestOrganizationMembershipCommand(organization.TaxId, organization.Country, "nuevo@org.cl", "Password1!", "Nuevo", "Usuario", null),
            CancellationToken.None);

        requested.IsSuccess.Should().BeTrue();
        var published = publisher.Published.Should().ContainSingle().Subject;
        published.UserId.Should().Be(admin.Id);
        published.Action.Should().Be(new NotificationAction(NotificationActionTypes.ApproveJoinRequest, requested.Value.UserId.ToString()));
        published.Link!.EntityType.Should().Be(NotificationEntityTypes.JoinRequest);

        // La bandeja guarda la acción; al aprobar la solicitud deja de estar disponible.
        var inboxItem = Add(NotificationTypes.JoinRequestReceived, admin.Id, NotificationModules.Organization,
            actionType: published.Action!.Type, actionTarget: published.Action.TargetId);
        var adminUser = AccessTestData.CurrentUser(admin, AccessPermissions.ApproveJoinRequests);
        var approved = await new ApproveJoinRequestCommandHandler(_db, adminUser, new FakeNotificationPublisher()).Handle(
            new ApproveJoinRequestCommand(requested.Value.UserId, RoleCodes.OrgOperator), CancellationToken.None);

        approved.IsSuccess.Should().BeTrue();
        inboxItem.ActionResolvedAt.Should().NotBeNull();
        NotificationInbox.ToDto(inboxItem).Action!.Available.Should().BeFalse();
    }

    [Fact]
    public async Task PaymentConfirmed_ShouldReferenceThePaymentAndItsBl()
    {
        var publisher = new FakeNotificationPublisher();
        var payment = new Payment
        {
            PaymentNumber = "PAY-1",
            PaymentType = "Cart",
            PaymentMethod = "KHIPU",
            TotalAmount = 100m,
            Currency = "CLP",
            Status = PaymentStatus.Confirmed,
            Country = CountryCodes.Chile,
            ReceiptNumber = "RCP-1",
            CreatedByUserId = _userId
        };
        var detail = new PaymentDetail { ConceptType = "THC", Currency = "CLP", Amount = 100m, BlNumber = "BL-9", PaymentId = payment.Id };

        await new NotifyPaymentStep(publisher).ExecuteAsync(payment, [detail], DateTime.UtcNow, CancellationToken.None);

        var request = publisher.Published.Should().ContainSingle().Subject;
        request.Link.Should().Be(new NotificationLink(NotificationEntityTypes.Payment, payment.Id.ToString(), "PAY-1", "BL-9"));
        request.Action.Should().Be(new NotificationAction(NotificationActionTypes.OpenPayment, payment.Id.ToString()));
    }

    [Fact]
    public async Task Preferences_ShouldListTheCatalog_AndPersistOverrides()
    {
        var handler = new UpdateNotificationPreferencesCommandHandler(_db, _currentUser);

        var updated = await handler.Handle(new UpdateNotificationPreferencesCommand(
        [
            new NotificationPreferenceChange(NotificationTypes.DocumentIssued, false),
            new NotificationPreferenceChange(NotificationTypes.AnnouncementPublished, true)
        ]), CancellationToken.None);

        updated.IsSuccess.Should().BeTrue();
        updated.Value.Should().HaveCount(NotificationTypes.Catalog.Count);
        var document = updated.Value.Single(p => p.Type == NotificationTypes.DocumentIssued);
        document.EmailEnabled.Should().BeFalse();
        document.IsCustomized.Should().BeTrue();
        updated.Value.Single(p => p.Type == NotificationTypes.AnnouncementPublished).EmailEnabled.Should().BeTrue();
        updated.Value.Single(p => p.Type == NotificationTypes.PaymentConfirmed).EmailEnabled.Should().BeTrue();   // por defecto
        _db.NotificationPreferenceList.Should().HaveCount(2).And.OnlyContain(p => p.UserId == _userId);

        // Volver al valor por defecto borra la preferencia.
        var reset = await handler.Handle(new UpdateNotificationPreferencesCommand(
            [new NotificationPreferenceChange(NotificationTypes.DocumentIssued, null)]), CancellationToken.None);

        reset.Value.Single(p => p.Type == NotificationTypes.DocumentIssued).EmailEnabled.Should().BeTrue();
        _db.NotificationPreferenceList.Should().ContainSingle();
    }

    [Theory]
    [InlineData(NotificationTypes.DeadlineOverdue, true, "Notification.EmailNotAvailable")]
    [InlineData(NotificationTypes.JoinRequestApproved, false, "Notification.EmailMandatory")]
    [InlineData("Unknown", true, "Notification.UnknownType")]
    public async Task Preferences_ShouldRejectWhatTheCatalogDoesNotAllow(string type, bool email, string error)
    {
        var result = await new UpdateNotificationPreferencesCommandHandler(_db, _currentUser).Handle(
            new UpdateNotificationPreferencesCommand([new NotificationPreferenceChange(type, email)]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(error);
        _db.NotificationPreferenceList.Should().BeEmpty();
    }

    [Fact]
    public void EmailPolicy_ShouldCombineCatalogAndPreference()
    {
        var optional = NotificationTypes.InfoOf(NotificationTypes.PaymentConfirmed);
        var mandatory = NotificationTypes.InfoOf(NotificationTypes.OrganizationApproved);
        var inboxOnly = NotificationTypes.InfoOf(NotificationTypes.DepositProofSubmitted);
        var announcement = NotificationTypes.InfoOf(NotificationTypes.AnnouncementPublished);

        NotificationEmailPolicy.Resolve(optional, null).Should().BeTrue();
        NotificationEmailPolicy.Resolve(optional, false).Should().BeFalse();
        NotificationEmailPolicy.Resolve(mandatory, false).Should().BeTrue();
        NotificationEmailPolicy.Resolve(inboxOnly, true).Should().BeFalse();
        NotificationEmailPolicy.Resolve(announcement, null).Should().BeFalse();
        NotificationEmailPolicy.Resolve(announcement, true).Should().BeTrue();
    }

    [Fact]
    public void Catalog_ShouldCoverEveryNotificationType()
    {
        var declared = typeof(NotificationTypes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        NotificationTypes.Catalog.Select(t => t.Type).Should().BeEquivalentTo(declared);
        NotificationTypes.Catalog.Should().OnlyContain(t => NotificationModules.All.Contains(t.Module));
    }
}
