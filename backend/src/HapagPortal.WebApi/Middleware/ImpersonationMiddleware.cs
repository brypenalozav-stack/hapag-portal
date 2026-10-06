namespace HapagPortal.WebApi.Middleware;

using System.Text.Json;
using HapagPortal.Application.Impersonation;
using HapagPortal.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Control en el servidor de la «Vista como cliente» (M8-08) para cada solicitud autenticada con un token de
/// impersonación, antes de llegar al controlador: la sesión debe seguir activa (401 <c>Impersonation.Ended</c> si terminó
/// o venció) y toda escritura no permitida se rechaza (403 <c>Impersonation.ReadOnly</c>). Cada solicitud, permitida o
/// bloqueada, queda en la auditoría con la identidad del usuario interno. Usa su propio ámbito de servicios para no
/// confirmar cambios pendientes de la solicitud.
/// </summary>
public sealed class ImpersonationMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        var claim = context.User.FindFirst(ImpersonationClaims.SessionId)?.Value;
        if (context.User.Identity?.IsAuthenticated != true || !Guid.TryParse(claim, out var sessionId))
        {
            await next(context);
            return;
        }

        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var address = context.Connection.RemoteIpAddress?.ToString();

        ImpersonationDecision decision;
        using (var scope = scopeFactory.CreateScope())
        {
            var guard = scope.ServiceProvider.GetRequiredService<ImpersonationGuard>();
            decision = await guard.CheckAsync(sessionId, method, path, context.RequestAborted);

            if (!decision.Allowed && !decision.SessionEnded)
                await guard.RecordAsync(sessionId, method, path, StatusCodes.Status403Forbidden, blocked: true, address, context.RequestAborted);
        }

        if (!decision.Allowed)
        {
            var status = decision.SessionEnded ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden;
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = status, Title = decision.Error!.Code, Detail = decision.Error.Message },
                JsonOptions,
                context.RequestAborted);
            return;
        }

        await next(context);

        using var recordScope = scopeFactory.CreateScope();
        await recordScope.ServiceProvider.GetRequiredService<ImpersonationGuard>()
            .RecordAsync(sessionId, method, path, context.Response.StatusCode, blocked: false, address, CancellationToken.None);
    }
}
