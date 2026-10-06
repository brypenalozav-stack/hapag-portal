using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace HapagPortal.Infrastructure.Authentication;

public sealed class JwtTokenService(
    IConfiguration configuration,
    ILogger<JwtTokenService> logger) : IJwtTokenService
{
    private const int DefaultExpirationMinutes = 60;
    private const int RefreshTokenSizeBytes = 64;

    public string GenerateToken(User user, IList<string> roles, IList<string>? permissions = null)
    {
        var expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var mins) ? mins : DefaultExpirationMinutes;
        return WriteToken(BuildClaims(user, roles, permissions), DateTime.UtcNow.AddMinutes(expirationMinutes));
    }

    public string GenerateImpersonationToken(
        User subject,
        IList<string> roles,
        IList<string> permissions,
        ImpersonationSession session)
    {
        var claims = BuildClaims(subject, roles, permissions);
        claims.Add(new Claim(ImpersonationClaims.SessionId, session.Id.ToString()));
        claims.Add(new Claim(ImpersonationClaims.ActorUserId, session.ActorUserId.ToString()));
        claims.Add(new Claim(ImpersonationClaims.ActorEmail, session.ActorEmail));
        return WriteToken(claims, session.ExpiresAt);
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured.");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static List<Claim> BuildClaims(User user, IList<string> roles, IList<string>? permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("country", user.Country),
        };

        if (user.ClientId.HasValue)
        {
            claims.Add(new Claim("clientId", user.ClientId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (permissions is not null)
        {
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        return claims;
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[RefreshTokenSizeBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string? GetEmailFromExpiredToken(string token)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured.");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateLifetime = false // Allow expired tokens
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract email from expired JWT token.");
            return null;
        }
    }
}
