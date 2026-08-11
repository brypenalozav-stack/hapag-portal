namespace HapagPortal.Application.Customs.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>
/// Ejecuta una transmisión contra el <see cref="ICustomsTransmitter"/> y aplica el acuse
/// a la entidad: actualiza estado, contador de intentos, folio y registra el evento del intento.
/// Centraliza la máquina de estados para no duplicarla en cada handler.
/// </summary>
internal static class TransmissionApplier
{
    public static async Task RunAsync(
        CustomsTransmission transmission,
        string referenceKey,
        string payload,
        ICustomsTransmitter transmitter,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        transmission.Status = CustomsTransmissionStatus.Queued;
        transmission.AttemptCount += 1;
        transmission.SubmittedAt = DateTime.UtcNow;

        var request = new CustomsTransmissionRequest(
            transmission.Stage, transmission.Kind, referenceKey, payload);

        CustomsTransmissionResult result;
        try
        {
            result = await transmitter.TransmitAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            transmission.Status = CustomsTransmissionStatus.Error;
            transmission.ResponseCode = "EXCEPTION";
            transmission.ResponseMessage = ex.Message;
            transmission.RespondedAt = DateTime.UtcNow;
            AddEvent(transmission, dbContext);
            return;
        }

        transmission.Status = result.Accepted
            ? CustomsTransmissionStatus.Accepted
            : CustomsTransmissionStatus.Rejected;
        transmission.ResponseCode = result.ResponseCode;
        transmission.ResponseMessage = result.ResponseMessage;
        transmission.Reference = result.Reference;
        transmission.RespondedAt = DateTime.UtcNow;

        AddEvent(transmission, dbContext);
    }

    private static void AddEvent(CustomsTransmission transmission, IApplicationDbContext dbContext) =>
        dbContext.CustomsTransmissionEvents.Add(new CustomsTransmissionEvent
        {
            TransmissionId = transmission.Id,
            Attempt = transmission.AttemptCount,
            Status = transmission.Status,
            ResponseCode = transmission.ResponseCode,
            ResponseMessage = transmission.ResponseMessage,
            OccurredAt = DateTime.UtcNow
        });
}
