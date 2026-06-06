namespace HapagPortal.Application.Common.Dtos;

public sealed record BLContainerDto(
    Guid Id,
    string ContainerNumber,
    string ContainerType,
    string? SealNumber,
    decimal? Weight,
    string Status);
