namespace HapagPortal.Application.Common.Dtos;

public sealed record PaymentMethodResponseDto(
    string Code,
    string Name,
    string Description,
    bool IsActive);
