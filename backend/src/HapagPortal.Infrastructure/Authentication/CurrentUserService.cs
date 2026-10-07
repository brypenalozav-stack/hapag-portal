using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace HapagPortal.Infrastructure.Authentication;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid? ClientId
    {
        get
        {
            var value = User?.FindFirstValue("clientId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.FindFirstValue(ClaimTypes.Email);

    public string? Country =>
        User?.FindFirstValue("country");

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList().AsReadOnly()
        ?? (IReadOnlyList<string>)[];

    public IReadOnlyList<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value).ToList().AsReadOnly()
        ?? (IReadOnlyList<string>)[];

    public bool HasPermission(string permission) =>
        User?.FindAll("permission").Any(c => c.Value == permission) ?? false;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    // M8-08: el token de «Vista como cliente» identifica la sesión y al usuario interno que la inició.
    public Guid? ImpersonationSessionId =>
        Guid.TryParse(User?.FindFirstValue(ImpersonationClaims.SessionId), out var id) ? id : null;

    public Guid? ImpersonatorUserId =>
        Guid.TryParse(User?.FindFirstValue(ImpersonationClaims.ActorUserId), out var id) ? id : null;

    public bool IsImpersonating => ImpersonationSessionId is not null;

    // M3-17: la clave del canal Web Service autentica al usuario técnico de su cliente.
    public Guid? ApiClientId =>
        Guid.TryParse(User?.FindFirstValue(ApiClientClaims.ClientId), out var id) ? id : null;

    // UseForwardedHeaders ya resolvió la IP del cliente detrás del proxy.
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
