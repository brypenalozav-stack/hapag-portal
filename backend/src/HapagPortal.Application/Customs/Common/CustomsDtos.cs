namespace HapagPortal.Application.Customs.Common;

public sealed record ManifestDto(
    Guid Id,
    string VesselImo,
    string Voyage,
    string Port,
    string Direction,
    DateTime? EstimatedArrival,
    DateTime? EstimatedDeparture,
    int TransmissionCount);

public sealed record TransmissionDto(
    Guid Id,
    Guid? ManifestId,
    Guid? BillOfLadingId,
    string? BLNumber,
    string Stage,
    string Kind,
    string Status,
    string? Reference,
    string? ResponseCode,
    string? ResponseMessage,
    int AttemptCount,
    DateTime? SubmittedAt,
    DateTime? RespondedAt);
