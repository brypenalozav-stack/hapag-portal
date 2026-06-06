namespace HapagPortal.Application.Common.Dtos;

public sealed record DemurrageChargeDto(
    Guid Id,
    Guid BlId,
    string BlNumber,
    string ContainerNumber,
    int FreeDays,
    int DemurrageDays,
    decimal DailyRate,
    decimal TotalAmount,
    string Currency,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    bool IsExempt,
    string? ExemptionReason,
    string Country);
