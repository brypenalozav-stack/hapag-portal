namespace HapagPortal.Application.Common.Dtos;

public sealed record BillOfLadingResponseDto(
    Guid Id,
    string BLNumber,
    string Type,
    string? Vessel,
    string? Voyage,
    string? PortOfLoading,
    string? PortOfDischarge,
    string? PlaceOfDelivery,
    DateTime? ETD,
    DateTime? ETA,
    decimal FreightAmount,
    string FreightCurrency,
    string FreightStatus,
    string Status,
    string Country,
    Guid ClientId,
    string? ClientName,
    DateTime CreatedAt,
    List<BLContainerDto>? Containers);
