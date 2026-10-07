using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// URL públicas del portal para las pasarelas: <c>Payments:PublicBaseUrl</c> (sitio, adonde vuelve el pagador) y
/// <c>Payments:ApiPublicBaseUrl</c> (API, adonde notifica la pasarela; por defecto la misma). Las pasarelas exigen URL
/// absolutas; sin la URL base configurada, el cobro no se inicia (<c>Integration.NotConfigured</c>).
/// </summary>
public sealed class PaymentPublicUrls(string? publicBaseUrl, string? apiPublicBaseUrl)
{
    public const string PublicBaseUrlKey = "Payments:PublicBaseUrl";
    public const string ApiPublicBaseUrlKey = "Payments:ApiPublicBaseUrl";

    private readonly string? _site = Normalize(publicBaseUrl);
    private readonly string? _api = Normalize(apiPublicBaseUrl) ?? Normalize(publicBaseUrl);

    /// <summary>URL del sitio (retorno del pagador).</summary>
    public Result<string> Site(string url) => Absolute(url, _site, PublicBaseUrlKey);

    /// <summary>URL de la API (notificación de la pasarela).</summary>
    public Result<string> Api(string url) => Absolute(url, _api, ApiPublicBaseUrlKey);

    private static Result<string> Absolute(string url, string? baseUrl, string key)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https")
            return Result<string>.Success(url);

        return baseUrl is null
            ? Result<string>.Failure(DomainErrors.Integration.NotConfigured(key))
            : Result<string>.Success(baseUrl + "/" + url.TrimStart('/'));
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) || !Uri.TryCreate(trimmed, UriKind.Absolute, out _) ? null : trimmed.TrimEnd('/');
    }
}

/// <summary>
/// Envío HTTP común de las pasarelas. Lee la respuesta como JSON genérico (las pasarelas varían tipos: un monto puede
/// llegar como número o como texto) y traduce el resultado: 2xx → cuerpo; 5xx, 408, 429, circuito abierto o error de
/// red → <c>Integration.Unavailable</c>; tiempo agotado → <c>Integration.Timeout</c>; otro 4xx (credenciales
/// rechazadas, solicitud inválida, pago inexistente) o cuerpo que no es JSON → <c>Integration.InvalidResponse</c>.
/// Nunca registra cuerpos (llevan credenciales o firmas).
/// </summary>
public static class GatewayHttp
{
    public static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<Result<JsonElement>> SendAsync(
        HttpClient client,
        string system,
        string operation,
        HttpMethod method,
        string relativeUri,
        CancellationToken cancellationToken,
        object? body = null,
        JsonSerializerOptions? json = null,
        IEnumerable<KeyValuePair<string, string>>? headers = null,
        AuthenticationHeaderValue? authorization = null)
    {
        using var request = new HttpRequestMessage(method, relativeUri.TrimStart('/'));
        if (body is not null)
        {
            var payload = JsonSerializer.Serialize(body, body.GetType(), json ?? CamelCase);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        foreach (var (name, value) in headers ?? [])
            request.Headers.TryAddWithoutValidation(name, value);
        if (authorization is not null)
            request.Headers.Authorization = authorization;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Options.Set(IntegrationLoggingHandler.OperationKey, operation);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<JsonElement>.Failure(IsTransient(response.StatusCode)
                    ? DomainErrors.Integration.Unavailable(system)
                    : DomainErrors.Integration.InvalidResponse(system));
            }

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
                return Result<JsonElement>.Success(default);

            using var document = JsonDocument.Parse(text);
            return Result<JsonElement>.Success(document.RootElement.Clone());
        }
        catch (TimeoutRejectedException)
        {
            return Result<JsonElement>.Failure(DomainErrors.Integration.Timeout(system));
        }
        catch (BrokenCircuitException)
        {
            return Result<JsonElement>.Failure(DomainErrors.Integration.Unavailable(system));
        }
        catch (HttpRequestException)
        {
            return Result<JsonElement>.Failure(DomainErrors.Integration.Unavailable(system));
        }
        catch (JsonException)
        {
            return Result<JsonElement>.Failure(DomainErrors.Integration.InvalidResponse(system));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<JsonElement>.Failure(DomainErrors.Integration.Timeout(system));
        }
    }

    public static bool IsTransient(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 || statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout;
}

/// <summary>Lectura tolerante de JSON de las pasarelas: rutas con puntos, texto o número indistintamente.</summary>
public static class GatewayJson
{
    /// <summary>Elemento en la ruta (<c>"a.b.c"</c>), o nulo si falta alguna parte o es null.</summary>
    public static JsonElement? Find(JsonElement root, string path)
    {
        var current = root;
        foreach (var part in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !TryGetProperty(current, part, out current))
                return null;
        }

        return current.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : current;
    }

    /// <summary>Primer valor de texto entre las rutas (los números se devuelven con su texto original).</summary>
    public static string? Text(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (Find(root, path) is not { } value)
                continue;

            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    /// <summary>Primer monto entre las rutas: número o texto con punto decimal ("1000.0000").</summary>
    public static decimal? Decimal(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (Find(root, path) is not { } value)
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return number;

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }
}

/// <summary>Montos y firmas comunes a las pasarelas.</summary>
public static class GatewayFormat
{
    /// <summary>
    /// Monto para la pasarela: entero en las monedas sin decimales (CLP), con hasta dos
    /// decimales y sin ceros sobrantes en el resto. Se envía como número JSON.
    /// </summary>
    public static decimal Amount(decimal amount, string currency) =>
        IsZeroDecimal(currency)
            ? decimal.Round(amount, 0, MidpointRounding.AwayFromZero)
            : decimal.Parse(
                decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);

    /// <summary>Texto invariable del monto (sin separador de miles, punto decimal), para firmar.</summary>
    public static string AmountText(decimal amount, string currency) =>
        Amount(amount, currency).ToString(IsZeroDecimal(currency) ? "0" : "0.##", CultureInfo.InvariantCulture);

    public static bool IsZeroDecimal(string currency) =>
        currency.Equals("CLP", StringComparison.OrdinalIgnoreCase) || currency.Equals("PYG", StringComparison.OrdinalIgnoreCase);

    /// <summary>Comparación en tiempo constante de dos textos (UTF-8).</summary>
    public static bool FixedTimeEquals(string expected, string? provided) =>
        provided is not null &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided));

    /// <summary>Texto truncado a la longitud que admite la pasarela.</summary>
    public static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>Cabeceras de la notificación sin distinguir mayúsculas.</summary>
    public static string? Header(IReadOnlyDictionary<string, string> headers, string name)
    {
        if (headers.TryGetValue(name, out var direct))
            return direct;

        foreach (var (key, value) in headers)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                return value;
        }

        return null;
    }
}
