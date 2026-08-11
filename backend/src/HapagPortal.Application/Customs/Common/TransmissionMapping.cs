namespace HapagPortal.Application.Customs.Common;

using HapagPortal.Domain.Entities;

internal static class TransmissionMapping
{
    public static TransmissionDto ToDto(this CustomsTransmission t, string? blNumber) =>
        new(t.Id, t.ManifestId, t.BillOfLadingId, blNumber, t.Stage, t.Kind, t.Status,
            t.Reference, t.ResponseCode, t.ResponseMessage, t.AttemptCount, t.SubmittedAt, t.RespondedAt);
}
