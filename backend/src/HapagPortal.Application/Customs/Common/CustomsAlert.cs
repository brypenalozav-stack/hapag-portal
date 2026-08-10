namespace HapagPortal.Application.Customs.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>Publica una notificación al Supervisor cuando una transmisión no resulta aceptada.</summary>
internal static class CustomsAlert
{
    public static async Task NotifyIfNotAcceptedAsync(
        CustomsTransmission transmission,
        string target,
        INotificationPublisher? publisher,
        CancellationToken cancellationToken)
    {
        if (publisher is null)
            return;
        if (transmission.Status is not (CustomsTransmissionStatus.Rejected or CustomsTransmissionStatus.Error))
            return;

        var type = transmission.Status == CustomsTransmissionStatus.Rejected
            ? NotificationTypes.TransmissionRejected
            : NotificationTypes.TransmissionError;

        await publisher.PublishAsync(new NotificationRequest(
            Type: type,
            Title: $"Transmisión {transmission.Status.ToLowerInvariant()}: {target}",
            Body: $"{transmission.ResponseCode}: {transmission.ResponseMessage}",
            RoleCode: RoleCodes.Supervisor,
            DedupKey: $"tx:{transmission.Id}:{transmission.Status}:{transmission.AttemptCount}"),
            cancellationToken);
    }
}
