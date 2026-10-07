namespace HapagPortal.Application.Common.Dtos;

using HapagPortal.Application.Organizations.Common;

public sealed record AuthResponseDto(
    string Token,
    int ExpiresIn,
    ClientResponseDto? User,
    string? RefreshToken = null,
    OrganizationSummaryDto? Organization = null);
