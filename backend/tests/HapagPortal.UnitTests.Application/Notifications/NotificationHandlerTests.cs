namespace HapagPortal.UnitTests.Application.Notifications;

using FluentAssertions;
using HapagPortal.Application.Notifications.Alerts;
using HapagPortal.Application.Notifications.MarkRead;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class NotificationHandlerTests
{
    private readonly MockApplicationDbContext _db = new();

    [Fact]
    public async Task MarkRead_SetsReadAt()
    {
        var n = new Notification
        {
            Id = Guid.NewGuid(), Type = NotificationTypes.DeadlineOverdue,
            Title = "t", Body = "b"
        };
        _db.NotificationList.Add(n);
        var handler = new MarkNotificationReadCommandHandler(_db);

        var result = await handler.Handle(new MarkNotificationReadCommand(n.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        n.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkRead_NotFound_Fails()
    {
        var handler = new MarkNotificationReadCommandHandler(_db);

        var result = await handler.Handle(new MarkNotificationReadCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateDeadlineAlerts_PublishesForOverdue_NotForOnTrack()
    {
        var overdueRule = new DeadlineRule
        {
            Id = Guid.NewGuid(), Code = "R1", Name = "Encabezado",
            BaseEvent = DeadlineBaseEvents.ArrivalEstimated, OffsetHours = -48, AtRiskWindowHours = 12,
            Severity = DeadlineSeverity.High, Certainty = DeadlineCertainty.Confirmed
        };
        var onTrackRule = new DeadlineRule
        {
            Id = Guid.NewGuid(), Code = "R2", Name = "Futuro",
            BaseEvent = DeadlineBaseEvents.ArrivalEstimated, OffsetHours = -48, AtRiskWindowHours = 12,
            Severity = DeadlineSeverity.Low, Certainty = DeadlineCertainty.Confirmed
        };

        _db.DeadlineInstanceList.Add(new DeadlineInstance
        {
            Id = Guid.NewGuid(), RuleId = overdueRule.Id, Rule = overdueRule,
            BaseEventAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DueAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),  // en el pasado -> vencido
            Status = DeadlineStatus.Overdue
        });
        _db.DeadlineInstanceList.Add(new DeadlineInstance
        {
            Id = Guid.NewGuid(), RuleId = onTrackRule.Id, Rule = onTrackRule,
            BaseEventAt = new DateTime(2999, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DueAt = new DateTime(2999, 1, 1, 0, 0, 0, DateTimeKind.Utc),  // futuro lejano -> en plazo
            Status = DeadlineStatus.OnTrack
        });

        var publisher = new FakeNotificationPublisher();
        var handler = new GenerateDeadlineAlertsCommandHandler(_db, publisher);

        var result = await handler.Handle(new GenerateDeadlineAlertsCommand(), CancellationToken.None);

        result.Value.Should().Be(1);
        publisher.Published.Should().ContainSingle();
        publisher.Published[0].Type.Should().Be(NotificationTypes.DeadlineOverdue);
        publisher.Published[0].RoleCode.Should().Be(RoleCodes.Supervisor);
    }
}
