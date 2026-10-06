namespace HapagPortal.WebApi.Abstractions;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HapagPortal.Application.WebService;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

/// <summary>Entidad del portal creada por una operación del canal; el controlador la deja en <c>HttpContext.Items</c>.</summary>
public sealed record WebServiceTarget(string Type, Guid Id, string? Reference, string? BlNumber)
{
    public const string Item = "ws.target";
}

/// <summary>
/// Control de cada operación del canal Web Service (M3-17), después de autenticar la clave: alcance del cliente (403
/// <c>WebService.ScopeNotGranted</c>), límite por cliente (429), clave de idempotencia de las operaciones que crean (400 sin
/// ella, 409 si se reusa con otro contenido o la primera sigue en proceso, y la misma respuesta ante un reintento idéntico,
/// con <c>Idempotent-Replayed: true</c>) y registro en la bitácora del canal (NF-14) con el resultado y lo creado. La bitácora
/// usa su propio ámbito de servicios, para no confirmar cambios pendientes de la operación.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class WebServiceOperationAttribute(string operation) : Attribute, IAsyncActionFilter
{
    public const string IdempotencyHeader = "Idempotency-Key";
    public const string ReplayedHeader = "Idempotent-Replayed";

    public string Operation { get; } = operation;

    /// <summary>Alcance del cliente que exige la operación (<c>ApiClientScopes</c>); nulo = cualquiera.</summary>
    public string? Scope { get; init; }

    /// <summary>La operación crea una solicitud: exige <c>Idempotency-Key</c> y se lista como solicitud del cliente.</summary>
    public bool Creates { get; init; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (http.Items[ApiKeyDefaults.IdentityItem] is not ApiClientIdentity identity)
        {
            context.Result = Problem(StatusCodes.Status401Unauthorized, DomainErrors.ApiClient.InvalidKey);
            return;
        }

        var scopeFactory = http.RequestServices.GetRequiredService<IServiceScopeFactory>();
        var jsonOptions = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        var start = new ApiClientRequestStart(
            identity,
            Operation,
            http.Request.Method,
            http.Request.Path.Value ?? string.Empty,
            Creates ? http.Request.Headers[IdempotencyHeader].ToString() : null,
            Creates ? Hash(Operation, context.ActionArguments, jsonOptions) : null,
            http.Connection.RemoteIpAddress?.ToString(),
            Creates,
            Scope);

        ApiClientRequestGate gate;
        using (var scope = scopeFactory.CreateScope())
        {
            gate = await scope.ServiceProvider.GetRequiredService<ApiClientRequestLog>().BeginAsync(start, DateTime.UtcNow, http.RequestAborted);
        }

        if (gate.Error is not null)
        {
            context.Result = Problem(gate.StatusCode, gate.Error);
            return;
        }

        if (gate.IsReplay)
        {
            http.Response.Headers[ReplayedHeader] = "true";
            context.Result = new ContentResult { Content = gate.ReplayJson, ContentType = "application/json", StatusCode = gate.StatusCode };
            return;
        }

        var executed = await next();

        int status;
        string? errorCode = null;
        string? json = null;
        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            status = StatusCodes.Status500InternalServerError;
            errorCode = "Error.Unexpected";
        }
        else
        {
            switch (executed.Result)
            {
                case ObjectResult objectResult:
                    status = objectResult.StatusCode ?? StatusCodes.Status200OK;
                    json = JsonSerializer.Serialize(objectResult.Value, jsonOptions);
                    break;
                case IStatusCodeActionResult statusResult:
                    status = statusResult.StatusCode ?? StatusCodes.Status200OK;
                    break;
                default:
                    status = StatusCodes.Status200OK;
                    break;
            }
            if (executed.Result is ObjectResult { Value: ProblemDetails problem })
                errorCode = problem.Title;
        }

        var target = http.Items[WebServiceTarget.Item] as WebServiceTarget;
        using var completeScope = scopeFactory.CreateScope();
        await completeScope.ServiceProvider.GetRequiredService<ApiClientRequestLog>().CompleteAsync(
            gate.RequestId!.Value,
            new ApiClientRequestCompletion(status, errorCode, Creates ? json : null, target?.Type, target?.Id, target?.Reference, target?.BlNumber),
            DateTime.UtcNow,
            CancellationToken.None);
    }

    /// <summary>Huella de la operación y su contenido (argumentos enlazados, sin el token de cancelación).</summary>
    private static string Hash(string operation, IDictionary<string, object?> arguments, JsonSerializerOptions jsonOptions)
    {
        var content = string.Join("|", arguments
            .Where(a => a.Value is not CancellationToken)
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => $"{a.Key}={JsonSerializer.Serialize(a.Value, jsonOptions)}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{operation}|{content}"))).ToLowerInvariant();
    }

    private static ObjectResult Problem(int status, Error error)
    {
        var result = new ObjectResult(new ProblemDetails { Status = status, Title = error.Code, Detail = error.Message }) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
