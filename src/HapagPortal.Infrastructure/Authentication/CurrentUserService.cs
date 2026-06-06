using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HapagPortal.Application.Common.Interfaces;
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

    public string? Email =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.FindFirstValue(ClaimTypes.Email);

    public string? Country =>
        User?.FindFirstValue("country");

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList().AsReadOnly()
        ?? (IReadOnlyList<string>)[];

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}
