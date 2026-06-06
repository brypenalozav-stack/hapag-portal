namespace HapagPortal.Application.Common.Dtos;

public sealed record ServiceOrderResponseDto(
    Guid Id,
    string OrderNumber,
    string OrderType,
    string Status,
    string? Description,
    string Country,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    Guid BillOfLadingId,
    Guid ClientId);
