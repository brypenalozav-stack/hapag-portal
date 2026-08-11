namespace HapagPortal.Application.Auth.Common;

using HapagPortal.Domain.Entities;

public interface IJwtTokenService
{
    string GenerateToken(User user, IList<string> roles, IList<string>? permissions = null);
    string GenerateRefreshToken();
    string? GetEmailFromExpiredToken(string token);
}
