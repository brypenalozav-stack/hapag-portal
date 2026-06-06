namespace HapagPortal.Application.Common.Dtos;

public sealed record BLChargesResponseDto(
    string BLNumber,
    List<LocalChargeDto> LocalCharges,
    List<DemurrageChargeDto> DemurrageCharges);
