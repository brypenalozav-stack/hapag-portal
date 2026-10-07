using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HapagPortal.IntegrationSimulator;

/// <summary>Error de validación por campo (extensión <c>errors</c> de <c>Problem</c>).</summary>
public sealed record FieldError(string Field, string Message);

/// <summary>Cuerpo <c>Problem</c> (RFC 9457) común a todos los contratos.</summary>
public sealed record ProblemBody(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    string CorrelationId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<FieldError>? Errors = null);

/// <summary>
/// Cabeceras comunes de los contratos: <c>X-Correlation-Id</c> (se devuelve el recibido o uno nuevo) y
/// escenarios de falla con <c>X-Sim-Scenario</c>.
/// </summary>
public static partial class SimulatorHttp
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string ScenarioHeader = "X-Sim-Scenario";
    public const string ProblemContentType = "application/problem+json";

    private const string CorrelationIdItem = "Simulator.CorrelationId";

    public static readonly TimeSpan TimeoutDelay = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan SlowDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Middleware de escenarios: <c>error500</c> responde 500, <c>429</c> responde 429 con
    /// <c>Retry-After: 1</c>, <c>timeout</c> tarda 30 s y <c>lento</c> 3 s antes de responder normalmente.
    /// </summary>
    public static async Task ApplyScenarioAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();
        if (!ValidCorrelationId().IsMatch(correlationId))
            correlationId = Guid.NewGuid().ToString();

        context.Items[CorrelationIdItem] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Si el cliente corta la llamada durante la espera (timeout del lado del portal), la cancelación se
        // propaga: la solicitud queda abortada y no se completa con una respuesta vacía.
        switch (context.Request.Headers[ScenarioHeader].ToString().Trim().ToLowerInvariant())
        {
            case "error500":
                await Problem(context, StatusCodes.Status500InternalServerError, "Error interno",
                    "Escenario simulado error500.").ExecuteAsync(context);
                return;

            case "429":
                context.Response.Headers.RetryAfter = "1";
                await Problem(context, StatusCodes.Status429TooManyRequests, "Demasiadas solicitudes",
                    "Escenario simulado 429.").ExecuteAsync(context);
                return;

            case "timeout":
                await Task.Delay(TimeoutDelay, context.RequestAborted);
                break;

            case "lento":
                await Task.Delay(SlowDelay, context.RequestAborted);
                break;
        }

        await next(context);
    }

    public static IResult Problem(
        HttpContext context,
        int status,
        string title,
        string detail,
        IReadOnlyList<FieldError>? errors = null) =>
        Results.Json(
            new ProblemBody(
                "about:blank",
                title,
                status,
                detail,
                context.Request.PathBase.Add(context.Request.Path).ToUriComponent(),
                context.Items[CorrelationIdItem] as string ?? Guid.NewGuid().ToString(),
                errors),
            statusCode: status,
            contentType: ProblemContentType);

    public static IResult BadRequest(HttpContext context, RequestErrors errors) =>
        Problem(context, StatusCodes.Status400BadRequest, "Solicitud inválida",
            "Uno o más parámetros no cumplen el formato esperado.", errors.Items);

    public static IResult NotFound(HttpContext context) =>
        Problem(context, StatusCodes.Status404NotFound, "Recurso no encontrado",
            "No existe un recurso con el identificador indicado.");

    /// <summary>Longitud en puntos de código, como cuenta JSON Schema (no en unidades UTF-16).</summary>
    public static int CodePoints(string value) => value.EnumerateRunes().Count();

    public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [GeneratedRegex("^[\\x21-\\x7E]{1,64}$")]
    private static partial Regex ValidCorrelationId();
}

/// <summary>Acumula los errores de validación de una solicitud.</summary>
public sealed class RequestErrors
{
    private readonly List<FieldError> _items = [];

    public IReadOnlyList<FieldError> Items => _items;

    public bool Any => _items.Count > 0;

    public void Add(string field, string message) => _items.Add(new FieldError(field, message));
}

/// <summary>Lectura y validación de parámetros de consulta, ruta y cabecera según el contrato.</summary>
public static partial class SimRequest
{
    public static string? RequiredQuery(HttpContext context, string name, RequestErrors errors, int minLength = 0, int maxLength = int.MaxValue)
    {
        if (!context.Request.Query.TryGetValue(name, out var values))
        {
            errors.Add(name, "Es obligatorio.");
            return null;
        }

        return CheckLength(values.ToString(), name, errors, minLength, maxLength);
    }

    public static string? OptionalQuery(HttpContext context, string name) =>
        context.Request.Query.TryGetValue(name, out var values) ? values.ToString() : null;

    public static string? CheckLength(string value, string name, RequestErrors errors, int minLength = 0, int maxLength = int.MaxValue)
    {
        var length = SimulatorHttp.CodePoints(value);
        if (length < minLength || length > maxLength)
        {
            errors.Add(name, $"Debe tener entre {minLength} y {maxLength} caracteres.");
            return null;
        }

        return value;
    }

    public static string? CheckEnum(string? value, string name, RequestErrors errors, params string[] allowed)
    {
        if (value is null)
            return null;

        if (!allowed.Contains(value, StringComparer.Ordinal))
        {
            errors.Add(name, $"Valores admitidos: {string.Join(", ", allowed)}.");
            return null;
        }

        return value;
    }

    public static string? CheckCurrency(string? value, string name, RequestErrors errors)
    {
        if (value is null)
            return null;

        if (!CurrencyPattern().IsMatch(value))
        {
            errors.Add(name, "Debe ser un código ISO 4217 de tres letras mayúsculas.");
            return null;
        }

        return value;
    }

    public static DateOnly? ParseDate(string? value, string name, RequestErrors errors)
    {
        if (value is null)
            return null;

        if (DatePattern().IsMatch(value) &&
            DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        errors.Add(name, "Debe ser una fecha yyyy-MM-dd.");
        return null;
    }

    public static DateTime? ParseDateTime(string? value, string name, RequestErrors errors)
    {
        if (value is null)
            return null;

        if (DateTimePattern().IsMatch(value) &&
            DateTimeOffset.TryParse(value.ToUpperInvariant(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed.UtcDateTime;
        }

        errors.Add(name, "Debe ser una fecha y hora RFC 3339.");
        return null;
    }

    public static int? ParseInt(string? value, string name, RequestErrors errors, int min, int max)
    {
        if (value is null)
            return null;

        if (int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) &&
            number >= min && number <= max)
        {
            return number;
        }

        errors.Add(name, $"Debe ser un entero entre {min} y {max}.");
        return null;
    }

    public static string? RequiredHeader(HttpContext context, string name, RequestErrors errors, int maxLength)
    {
        var value = context.Request.Headers[name].ToString();
        if (string.IsNullOrEmpty(value))
        {
            errors.Add(name, "Es obligatorio.");
            return null;
        }

        return CheckLength(value, name, errors, 1, maxLength);
    }

    [GeneratedRegex("^[A-Z]{3}\\z")]
    public static partial Regex CurrencyPattern();

    [GeneratedRegex("^[0-9]{7,8}-[0-9Kk]\\z")]
    public static partial Regex RutPattern();

    [GeneratedRegex("^[0-9]{4}-[0-9]{2}-[0-9]{2}\\z")]
    private static partial Regex DatePattern();

    [GeneratedRegex("^[0-9]{4}-[0-9]{2}-[0-9]{2}[Tt][0-9]{2}:[0-9]{2}:[0-9]{2}(\\.[0-9]+)?([Zz]|[+-][0-9]{2}:[0-9]{2})\\z")]
    private static partial Regex DateTimePattern();
}

/// <summary>Lectura y validación de un cuerpo JSON según el esquema del contrato.</summary>
public sealed class JsonBody(JsonElement element, string path, RequestErrors errors)
{
    public RequestErrors Errors { get; } = errors;

    /// <summary>Lee el cuerpo como objeto JSON. Cuerpo vacío, inválido o que no es objeto: null y error.</summary>
    public static async Task<(JsonDocument? Document, JsonBody? Body)> ReadAsync(HttpContext context, RequestErrors errors)
    {
        try
        {
            var document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return (document, new JsonBody(document.RootElement, string.Empty, errors));

            document.Dispose();
        }
        catch (JsonException)
        {
        }

        errors.Add("body", "Debe ser un objeto JSON.");
        return (null, null);
    }

    public string? RequiredString(string name, int maxLength = int.MaxValue) =>
        String(name, required: true, maxLength);

    public string? OptionalString(string name, int maxLength = int.MaxValue) =>
        String(name, required: false, maxLength);

    public decimal? RequiredNumber(string name, decimal? minimum = null, decimal? exclusiveMinimum = null)
    {
        if (!TryGet(name, required: true, out var value))
            return null;

        return Number(value, name, minimum, exclusiveMinimum);
    }

    public decimal? OptionalNumber(string name, bool nullable = false)
    {
        if (!TryGet(name, required: false, out var value))
            return null;

        if (nullable && value.ValueKind == JsonValueKind.Null)
            return null;

        return Number(value, name, null, null);
    }

    public int? RequiredInteger(string name, int minimum = int.MinValue)
    {
        if (!TryGet(name, required: true, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) &&
            number == decimal.Truncate(number) && number >= minimum && number <= int.MaxValue)
        {
            return (int)number;
        }

        Errors.Add(Field(name), $"Debe ser un entero mayor o igual a {minimum}.");
        return null;
    }

    public bool? RequiredBoolean(string name)
    {
        if (!TryGet(name, required: true, out var value))
            return null;

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return value.GetBoolean();

        Errors.Add(Field(name), "Debe ser booleano.");
        return null;
    }

    public JsonBody? RequiredObject(string name)
    {
        if (!TryGet(name, required: true, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Object)
            return new JsonBody(value, Field(name), Errors);

        Errors.Add(Field(name), "Debe ser un objeto.");
        return null;
    }

    public IReadOnlyList<JsonBody>? RequiredArrayOfObjects(string name, int minItems)
    {
        if (!TryGet(name, required: true, out var value))
            return null;

        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < minItems)
        {
            Errors.Add(Field(name), $"Debe ser una lista con al menos {minItems} elemento(s).");
            return null;
        }

        var items = new List<JsonBody>();
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var itemPath = $"{Field(name)}[{index++}]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                Errors.Add(itemPath, "Debe ser un objeto.");
                continue;
            }

            items.Add(new JsonBody(item, itemPath, Errors));
        }

        return items;
    }

    private string? String(string name, bool required, int maxLength)
    {
        if (!TryGet(name, required, out var value))
            return null;

        if (value.ValueKind != JsonValueKind.String)
        {
            Errors.Add(Field(name), "Debe ser texto.");
            return null;
        }

        return SimRequest.CheckLength(value.GetString()!, Field(name), Errors, 0, maxLength);
    }

    private decimal? Number(JsonElement value, string name, decimal? minimum, decimal? exclusiveMinimum)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) &&
            (minimum is null || number >= minimum) &&
            (exclusiveMinimum is null || number > exclusiveMinimum))
        {
            return number;
        }

        Errors.Add(Field(name), "Debe ser un número válido.");
        return null;
    }

    private bool TryGet(string name, bool required, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;

        if (required)
            Errors.Add(Field(name), "Es obligatorio.");

        return false;
    }

    private string Field(string name) => path.Length == 0 ? name : $"{path}.{name}";
}
