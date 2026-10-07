namespace HapagPortal.Application.Auth.Common;

using HapagPortal.Domain.Entities;

public interface IJwtTokenService
{
    string GenerateToken(User user, IList<string> roles, IList<string>? permissions = null);
    string GenerateRefreshToken();
    string? GetEmailFromExpiredToken(string token);

    /// <summary>
    /// Token de una sesión de «Vista como cliente» (M8-08): el sujeto es el usuario cliente, con sus roles y permisos, y
    /// los claims de <c>ImpersonationClaims</c> identifican la sesión y al actor interno. Vence con la sesión y no tiene
    /// refresh token: no toca las credenciales del cliente.
    /// </summary>
    string GenerateImpersonationToken(
        User subject,
        IList<string> roles,
        IList<string> permissions,
        ImpersonationSession session);
}
