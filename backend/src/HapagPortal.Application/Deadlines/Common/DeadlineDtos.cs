namespace HapagPortal.Application.Deadlines.Common;

public sealed record DeadlineRuleDto(
    Guid Id,
    string Code,
    string Name,
    string BaseEvent,
    int OffsetHours,
    int AtRiskWindowHours,
    string? Direction,
    string? Country,
    string? BLType,
    string Severity,
    string? Source,
    string Certainty,
    bool IsActive);

public sealed record DeadlineDashboardItemDto(
    Guid Id,
    string RuleCode,
    string RuleName,
    string Severity,
    Guid? ManifestId,
    Guid? BillOfLadingId,
    string? BLNumber,
    DateTime BaseEventAt,
    DateTime DueAt,
    DateTime? CompletedAt,
    string Status);
