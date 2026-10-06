namespace HapagPortal.Application.WebService;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Datos de la solicitud del canal que se registran antes de ejecutarla.</summary>
public sealed record ApiClientRequestStart(
    ApiClientIdentity Identity,
    string Operation,
    string Method,
    string Path,
    string? IdempotencyKey,
    string? RequestHash,
    string? SourceAddress,
    bool RequiresIdempotencyKey,
    string? RequiredScope = null);

/// <summary>
/// Decisión antes de ejecutar: continuar (con la fila de la bitácora), rechazar (límite por cliente o clave de idempotencia
/// inválida o en conflicto, con su código HTTP) o repetir la respuesta guardada de la misma clave de idempotencia.
/// </summary>
public sealed record ApiClientRequestGate(
    Guid? RequestId,
    Error? Error,
    int StatusCode,
    string? ReplayJson)
{
    public bool Proceed => Error is null && ReplayJson is null;
    public bool IsReplay => ReplayJson is not null;
}

/// <summary>Resultado de la solicitud ejecutada y entidad del portal que creó.</summary>
public sealed record ApiClientRequestCompletion(
    int StatusCode,
    string? ErrorCode,
    string? ResponseJson,
    string? TargetType,
    Guid? TargetId,
    string? TargetReference,
    string? BlNumber);

/// <summary>
/// Bitácora, límite por cliente e idempotencia del canal Web Service (M3-17, NF-14, NF-19):
/// <list type="bullet">
/// <item>Alcance: la operación debe estar entre los alcances habilitados al cliente (403 <c>WebService.ScopeNotGranted</c>).</item>
/// <item>Límite: solicitudes registradas del cliente en los últimos 60 segundos frente a su <c>RateLimitPerMinute</c>
/// (429 <c>WebService.RateLimited</c>; el rechazo también se registra).</item>
/// <item>Idempotencia: las operaciones que crean exigen <c>Idempotency-Key</c>; un reintento con la misma clave y el mismo
/// contenido devuelve la respuesta guardada (aceptada o rechazada) sin ejecutar de nuevo; con otro contenido, 409; mientras
/// la primera se procesa, 409; una falla del portal (5xx) se puede reintentar con la misma clave.</item>
/// </list>
/// Debe usarse con su propio contexto de datos (un ámbito aparte), para no confirmar cambios pendientes de la solicitud.
/// </summary>
public sealed class ApiClientRequestLog(IApplicationDbContext dbContext)
{
    public const int MaxIdempotencyKeyLength = 100;
    private const int MaxResponseLength = 200_000;

    public async Task<ApiClientRequestGate> BeginAsync(ApiClientRequestStart start, DateTime now, CancellationToken cancellationToken)
    {
        var identity = start.Identity;
        var since = now.AddSeconds(-60);
        var recent = await dbContext.ApiClientRequests.CountAsync(r => r.ApiClientId == identity.ApiClientId && r.ReceivedAt >= since, cancellationToken);
        if (recent >= identity.RateLimitPerMinute)
            return await RejectAsync(start, DomainErrors.ApiClient.RateLimited, 429, now, cancellationToken);

        if (start.RequiredScope is not null && !identity.Scopes.Contains(start.RequiredScope))
            return await RejectAsync(start, DomainErrors.ApiClient.ScopeNotGranted(start.RequiredScope), 403, now, cancellationToken);

        var key = string.IsNullOrWhiteSpace(start.IdempotencyKey) ? null : start.IdempotencyKey.Trim();
        if (start.RequiresIdempotencyKey && (key is null || key.Length > MaxIdempotencyKeyLength))
            return await RejectAsync(start, DomainErrors.ApiClient.IdempotencyKeyRequired, 400, now, cancellationToken);
        if (!start.RequiresIdempotencyKey)
            key = null;

        if (key is not null)
        {
            var previous = await dbContext.ApiClientRequests
                .FirstOrDefaultAsync(r => r.ApiClientId == identity.ApiClientId && r.IdempotencyKey == key, cancellationToken);
            if (previous is not null)
            {
                if (!string.Equals(previous.RequestHash, start.RequestHash, StringComparison.Ordinal) || previous.Operation != start.Operation)
                    return await RejectAsync(start with { IdempotencyKey = null }, DomainErrors.ApiClient.IdempotencyKeyReused, 409, now, cancellationToken);

                if (previous.Outcome == ApiClientRequestOutcomes.Processing)
                    return new ApiClientRequestGate(null, DomainErrors.ApiClient.IdempotencyInProgress, 409, null);

                if (previous.Outcome != ApiClientRequestOutcomes.Failed)
                    return new ApiClientRequestGate(previous.Id, null, previous.StatusCode ?? 200, previous.ResponseJson ?? "null");

                // Falla del portal: el reintento con la misma clave vuelve a ejecutar la operación sobre la misma fila.
                previous.Outcome = ApiClientRequestOutcomes.Processing;
                previous.ReceivedAt = now;
                previous.CompletedAt = null;
                previous.StatusCode = null;
                previous.ErrorCode = null;
                previous.ApiClientKeyId = identity.KeyId;
                await dbContext.SaveChangesAsync(cancellationToken);
                return new ApiClientRequestGate(previous.Id, null, 0, null);
            }
        }

        var row = NewRow(start with { IdempotencyKey = key }, ApiClientRequestOutcomes.Processing, now);
        dbContext.ApiClientRequests.Add(row);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Otra solicitud con la misma clave de idempotencia se registró al mismo tiempo (índice único).
            return new ApiClientRequestGate(null, DomainErrors.ApiClient.IdempotencyInProgress, 409, null);
        }

        return new ApiClientRequestGate(row.Id, null, 0, null);
    }

    public async Task CompleteAsync(Guid requestId, ApiClientRequestCompletion completion, DateTime now, CancellationToken cancellationToken)
    {
        var row = await dbContext.ApiClientRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (row is null)
            return;

        row.StatusCode = completion.StatusCode;
        row.ErrorCode = completion.ErrorCode;
        row.Outcome = completion.StatusCode switch
        {
            < 400 => ApiClientRequestOutcomes.Accepted,
            < 500 => ApiClientRequestOutcomes.Rejected,
            _ => ApiClientRequestOutcomes.Failed
        };
        row.ResponseJson = row.IdempotencyKey is null || completion.ResponseJson is null || completion.ResponseJson.Length > MaxResponseLength
            ? null
            : completion.ResponseJson;
        row.TargetType = completion.TargetType;
        row.TargetId = completion.TargetId;
        row.TargetReference = completion.TargetReference;
        row.BlNumber = completion.BlNumber ?? row.BlNumber;
        row.CompletedAt = now;
        row.DurationMs = (int)Math.Min(int.MaxValue, Math.Max(0, (now - row.ReceivedAt).TotalMilliseconds));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Registra y rechaza una solicitud antes de ejecutarla (alcance no habilitado, límite o idempotencia).</summary>
    public async Task<ApiClientRequestGate> RejectAsync(
        ApiClientRequestStart start,
        Error error,
        int statusCode,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var row = NewRow(start with { IdempotencyKey = null }, ApiClientRequestOutcomes.Rejected, now);
        row.StatusCode = statusCode;
        row.ErrorCode = error.Code;
        row.CompletedAt = now;
        row.DurationMs = 0;
        dbContext.ApiClientRequests.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ApiClientRequestGate(row.Id, error, statusCode, null);
    }

    private static ApiClientRequest NewRow(ApiClientRequestStart start, string outcome, DateTime now) => new()
    {
        ApiClientId = start.Identity.ApiClientId,
        ApiClientKeyId = start.Identity.KeyId,
        OrganizationId = start.Identity.OrganizationId,
        TechnicalUserId = start.Identity.UserId,
        Operation = start.Operation,
        Method = start.Method,
        Path = start.Path.Length > 300 ? start.Path[..300] : start.Path,
        IdempotencyKey = start.IdempotencyKey,
        RequestHash = start.RequestHash,
        Outcome = outcome,
        SourceAddress = start.SourceAddress,
        ReceivedAt = now
    };
}
