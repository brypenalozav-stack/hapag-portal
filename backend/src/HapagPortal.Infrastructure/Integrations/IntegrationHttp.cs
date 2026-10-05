using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace HapagPortal.Infrastructure.Integrations;

/// <summary>
/// Datos fijos de un sistema para los clientes Real: nombre (para errores y logs), tipo de secreto
/// de la API key, cabecera donde viaja y convención JSON del contrato.
/// </summary>
public sealed record IntegrationEndpoint(
    string System,
    string SecretType,
    string ApiKeyHeader,
    JsonSerializerOptions Json);

/// <summary>
/// Envío común de los clientes Real. Traduce el resultado HTTP a <c>Result</c>:
/// 2xx → cuerpo deserializado; 404 → <c>Success(null)</c>; 5xx, 408 y 429 (ya reintentados por la
/// tubería de resiliencia) y circuito abierto → <c>Integration.Unavailable</c>; tiempo agotado →
/// <c>Integration.Timeout</c>; otro 4xx o cuerpo que no cumple el contrato → <c>Integration.InvalidResponse</c>;
/// sin API key → <c>Integration.NotConfigured</c>.
/// </summary>
public static class IntegrationHttp
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>JSON camelCase de los contratos propios (CT-NEXUS, CT-FIS, CT-BCH, CT-DBNET, CT-TRACK).</summary>
    public static readonly JsonSerializerOptions CamelCaseJson = CreateJsonOptions(JsonNamingPolicy.CamelCase);

    /// <summary>JSON snake_case de la API pública de Khipu (CT-KHIPU).</summary>
    public static readonly JsonSerializerOptions SnakeCaseJson = CreateJsonOptions(JsonNamingPolicy.SnakeCaseLower);

    public static async Task<Result<T?>> SendAsync<T>(
        HttpClient client,
        ISecretResolver secretResolver,
        IntegrationEndpoint endpoint,
        string operation,
        HttpMethod method,
        string relativeUri,
        CancellationToken cancellationToken,
        object? body = null,
        string? idempotencyKey = null)
        where T : class
    {
        var apiKey = await secretResolver.ResolveAsync(endpoint.SecretType, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result<T?>.Failure(DomainErrors.Integration.NotConfigured(endpoint.System));

        using var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.TryAddWithoutValidation(endpoint.ApiKeyHeader, apiKey);
        if (idempotencyKey is not null)
            request.Headers.TryAddWithoutValidation(IdempotencyKeyHeader, idempotencyKey);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: endpoint.Json);
        request.Options.Set(IntegrationLoggingHandler.OperationKey, operation);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result<T?>.Success(null);

            if (!response.IsSuccessStatusCode)
                return Result<T?>.Failure(IsTransient(response.StatusCode)
                    ? DomainErrors.Integration.Unavailable(endpoint.System)
                    : DomainErrors.Integration.InvalidResponse(endpoint.System));

            var payload = await response.Content.ReadFromJsonAsync<T>(endpoint.Json, cancellationToken);

            return payload is null
                ? Result<T?>.Failure(DomainErrors.Integration.InvalidResponse(endpoint.System))
                : Result<T?>.Success(payload);
        }
        catch (TimeoutRejectedException)
        {
            return Result<T?>.Failure(DomainErrors.Integration.Timeout(endpoint.System));
        }
        catch (BrokenCircuitException)
        {
            return Result<T?>.Failure(DomainErrors.Integration.Unavailable(endpoint.System));
        }
        catch (HttpRequestException)
        {
            return Result<T?>.Failure(DomainErrors.Integration.Unavailable(endpoint.System));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Result<T?>.Failure(DomainErrors.Integration.InvalidResponse(endpoint.System));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout (no la tubería): también es tiempo agotado.
            return Result<T?>.Failure(DomainErrors.Integration.Timeout(endpoint.System));
        }
    }

    /// <summary>
    /// Arma una ruta relativa (sin "/" inicial, para conservar el prefijo de <c>BaseUrl</c>) con los
    /// parámetros de consulta no nulos, escapados.
    /// </summary>
    public static string WithQuery(string relativePath, params (string Name, string? Value)[] parameters)
    {
        var builder = new StringBuilder(relativePath.TrimStart('/'));
        var separator = '?';

        foreach (var (name, value) in parameters)
        {
            if (value is null)
                continue;

            builder.Append(separator)
                .Append(Uri.EscapeDataString(name))
                .Append('=')
                .Append(Uri.EscapeDataString(value));
            separator = '&';
        }

        return builder.ToString();
    }

    public static string Segment(string value) => Uri.EscapeDataString(value);

    private static bool IsTransient(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 || statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout;

    private static JsonSerializerOptions CreateJsonOptions(JsonNamingPolicy namingPolicy)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = namingPolicy,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Un campo obligatorio ausente o un null en un campo no anulable incumplen el contrato.
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
