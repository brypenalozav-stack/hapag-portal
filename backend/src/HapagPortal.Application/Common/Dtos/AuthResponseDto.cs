namespace HapagPortal.Application.Common.Dtos;

public sealed record AuthResponseDto(
    string Token,
    int ExpiresIn,
    ClientResponseDto? User,
    string? RefreshToken = null);
