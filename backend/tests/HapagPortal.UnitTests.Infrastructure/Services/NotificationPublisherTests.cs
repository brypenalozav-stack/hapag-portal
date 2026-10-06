namespace HapagPortal.UnitTests.Infrastructure.Services;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Notifications;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>
/// Publicación de notificaciones (M1-25) sobre EF real (InMemory): la bandeja guarda módulo, referencia y acción; el
/// correo se envía solo si el tipo lo admite y el destinatario no lo desactivó (salvo los obligatorios).
/// </summary>
public sealed class NotificationPublisherTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly NotificationPublisher _publisher;
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationPublisherTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _publisher = new NotificationPublisher(_context, _email, NullLogger<NotificationPublisher>.Instance);
    }

    private NotificationRequest Request(string type, string? dedup = null) =>
        new(type, "Título", "Cuerpo", UserId: _userId, DedupKey: dedup, Email: "user@org.cl",
            Link: new NotificationLink(NotificationEntityTypes.Payment, "p-1", "PAY-1", "BL-1"),
            Action: new NotificationAction(NotificationActionTypes.OpenPayment, "p-1"));

    private void Prefer(string type, bool email)
    {
        _context.NotificationPreferences.Add(new NotificationPreference { UserId = _userId, NotificationType = type, EmailEnabled = email });
        _context.SaveChanges();
    }

    [Fact]
    public async Task Publish_ShouldStoreModuleLinkAndAction_AndEmailByDefault()
    {
        await _publisher.PublishAsync(Request(NotificationTypes.PaymentConfirmed));

        var stored = await _context.Notifications.SingleAsync();
        stored.Module.Should().Be(NotificationModules.Payments);
        stored.EntityType.Should().Be(NotificationEntityTypes.Payment);
        stored.EntityReference.Should().Be("PAY-1");
        stored.BlNumber.Should().Be("BL-1");
        stored.ActionType.Should().Be(NotificationActionTypes.OpenPayment);
        stored.ActionTargetId.Should().Be("p-1");
        stored.EmailSent.Should().BeTrue();
        await _email.Received(1).SendEmailAsync("user@org.cl", "Título", "Cuerpo", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_WithEmailDisabledByTheUser_ShouldOnlyReachTheInbox()
    {
        Prefer(NotificationTypes.PaymentConfirmed, email: false);

        await _publisher.PublishAsync(Request(NotificationTypes.PaymentConfirmed));

        (await _context.Notifications.SingleAsync()).EmailSent.Should().BeFalse();
        await _email.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Publish_OptInType_ShouldEmailOnlyWhenTheUserEnabledIt()
    {
        await _publisher.PublishAsync(Request(NotificationTypes.AnnouncementPublished, "a-1"));
        await _email.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default!, default!, default);

        Prefer(NotificationTypes.AnnouncementPublished, email: true);
        await _publisher.PublishAsync(Request(NotificationTypes.AnnouncementPublished, "a-2"));
        await _email.Received(1).SendEmailAsync("user@org.cl", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_MandatoryType_ShouldEmailEvenIfDisabled_AndInboxOnlyTypeNever()
    {
        Prefer(NotificationTypes.JoinRequestApproved, email: false);

        await _publisher.PublishAsync(Request(NotificationTypes.JoinRequestApproved));
        await _publisher.PublishAsync(Request(NotificationTypes.DepositProofSubmitted));

        await _email.Received(1).SendEmailAsync("user@org.cl", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await _context.Notifications.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Publish_SameUnreadDedupKey_ShouldNotDuplicate()
    {
        await _publisher.PublishAsync(Request(NotificationTypes.PaymentConfirmed, "same"));
        await _publisher.PublishAsync(Request(NotificationTypes.PaymentConfirmed, "same"));

        (await _context.Notifications.CountAsync()).Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
