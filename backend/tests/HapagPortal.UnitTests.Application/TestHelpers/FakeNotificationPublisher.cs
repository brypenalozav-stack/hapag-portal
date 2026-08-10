namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;

/// <summary>Publicador de notificaciones que sólo acumula lo publicado, para verificar en tests.</summary>
public sealed class FakeNotificationPublisher : INotificationPublisher
{
    public List<NotificationRequest> Published { get; } = [];

    public Task PublishAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        Published.Add(request);
        return Task.CompletedTask;
    }
}
