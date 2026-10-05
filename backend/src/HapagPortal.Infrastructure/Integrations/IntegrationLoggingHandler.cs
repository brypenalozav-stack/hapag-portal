using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations;

/// <summary>
/// Registro de las llamadas a sistemas externos (NF-27). Va por fuera de la tubería de resiliencia, así
/// que ve una vez cada llamada lógica, con su resultado final:
/// <list type="bullet">
/// <item>propaga <c>X-Correlation-Id</c> si la solicitud ya lo trae; si no, usa el TraceId de la
/// actividad en curso o genera uno;</item>
/// <item>con respuesta ≥ 400 o excepción, registra en Warning <c>System</c>, <c>Operation</c>,
/// <c>StatusCode</c>, <c>DurationMs</c> y <c>CorrelationId</c>, e incrementa
/// <c>hapagportal.integrations.errors</c>.</item>
/// </list>
/// </summary>
public sealed class IntegrationLoggingHandler(
    string system,
    ILogger<IntegrationLoggingHandler> logger,
    IntegrationMetrics metrics) : DelegatingHandler
{
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>Nombre de la operación del contrato (operationId), fijado por el cliente en la solicitud.</summary>
    public static readonly HttpRequestOptionsKey<string> OperationKey = new("HapagPortal.Integrations.Operation");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = EnsureCorrelationId(request);
        var operation = request.Options.TryGetValue(OperationKey, out var op)
            ? op
            : $"{request.Method} {request.RequestUri?.AbsolutePath}";
        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if ((int)response.StatusCode >= 400)
            {
                metrics.RecordError(system);
                logger.LogWarning(
                    "Integración con error - System: {System}, Operation: {Operation}, StatusCode: {StatusCode}, DurationMs: {DurationMs}, CorrelationId: {CorrelationId}",
                    system, operation, (int)response.StatusCode, ElapsedMs(started), correlationId);
            }

            return response;
        }
        catch (Exception ex)
        {
            metrics.RecordError(system);
            logger.LogWarning(
                ex,
                "Integración con error - System: {System}, Operation: {Operation}, StatusCode: {StatusCode}, DurationMs: {DurationMs}, CorrelationId: {CorrelationId}",
                system, operation, null, ElapsedMs(started), correlationId);
            throw;
        }
    }

    private static string EnsureCorrelationId(HttpRequestMessage request)
    {
        if (request.Headers.TryGetValues(CorrelationIdHeader, out var values))
        {
            var existing = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(existing))
                return existing;

            request.Headers.Remove(CorrelationIdHeader);
        }

        var correlationId = Activity.Current is { } activity
            ? activity.TraceId.ToHexString()
            : Guid.NewGuid().ToString("N");

        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationId);
        return correlationId;
    }

    private static long ElapsedMs(long started) =>
        (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
