namespace HapagPortal.Application.Common.Dtos;

public sealed record WarehouseChangeResponseDto(
    Guid Id,
    string FromWarehouse,
    string ToWarehouse,
    decimal Amount,
    string Currency,
    string Status,
    string Country,
    Guid BillOfLadingId,
    DateTime CreatedAt);
