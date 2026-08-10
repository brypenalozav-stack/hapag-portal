using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HapagPortal.Infrastructure.Authentication;

public enum PermissionLogic
{
    All,
    Any,
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(
    PermissionLogic logic = PermissionLogic.All,
    params string[] permissions) : Attribute, IAsyncAuthorizationFilter
{
    public HasPermissionAttribute(params string[] permissions)
        : this(PermissionLogic.All, permissions)
    {
    }

    public string[] Permissions { get; } = permissions;
    public PermissionLogic Logic { get; } = logic;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        // Se evalúa contra el claim dedicado "permission" (no contra los roles).
        var granted = user.FindAll("permission").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasPermission = Logic switch
        {
            PermissionLogic.All => Permissions.All(granted.Contains),
            PermissionLogic.Any => Permissions.Any(granted.Contains),
            _ => false,
        };

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }

        return Task.CompletedTask;
    }
}
