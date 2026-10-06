using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.ServiceRequests;

/// <summary>Opción de un campo de selección.</summary>
public sealed record ServiceInputOption(string Value, string LabelEs, string LabelEn);

/// <summary>
/// Campo tipado del formulario de un servicio (<see cref="ServiceInputFieldTypes"/>). <c>Min</c>/<c>Max</c> y
/// <c>Integer</c> aplican a números; <c>MaxLength</c> a textos; <c>Options</c> a selecciones. Los campos
/// <c>file</c> se responden con adjuntos de la solicitud y <c>containers</c> con contenedores del BL.
/// </summary>
public sealed record ServiceInputField(
    string Key,
    string LabelEs,
    string LabelEn,
    string Type,
    bool Required = false,
    IReadOnlyList<ServiceInputOption>? Options = null,
    decimal? Min = null,
    decimal? Max = null,
    bool Integer = false,
    int? MaxLength = null,
    string? HelpEs = null,
    string? HelpEn = null);

/// <summary>Error de un campo (<c>Field</c> = clave del formulario) o del formulario completo.</summary>
public sealed record ServiceInputError(string Field, string Message);

/// <summary>Resultado de validar los datos: valores normalizados y contenedores elegidos.</summary>
public sealed record ServiceInputValidation(
    IReadOnlyList<ServiceInputError> Errors,
    string NormalizedJson,
    IReadOnlyList<string> Containers,
    IReadOnlyDictionary<string, decimal> Numbers)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Formulario tipado de las definiciones de servicio (M2-03, M2-04): lectura y validación del esquema que se
/// administra en el mantenedor, y de los datos que ingresa el cliente. Puro y determinista para pruebas.
/// </summary>
public static partial class ServiceInputSchema
{
    public const int MaxFields = 30;
    public const int DefaultTextLength = 500;
    public const int DefaultTextAreaLength = 4000;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [GeneratedRegex("^[a-z][a-zA-Z0-9]{0,39}$")]
    private static partial Regex KeyPattern();

    /// <summary>Lee el esquema JSON (lista de campos). Un JSON inválido devuelve nulo.</summary>
    public static IReadOnlyList<ServiceInputField>? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ServiceInputField>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string Serialize(IReadOnlyList<ServiceInputField> fields) =>
        JsonSerializer.Serialize(fields, JsonOptions);

    /// <summary>Valida la definición del formulario: claves únicas, tipos conocidos, opciones y rangos.</summary>
    public static IReadOnlyList<string> ValidateSchema(IReadOnlyList<ServiceInputField> fields)
    {
        var errors = new List<string>();

        if (fields.Count > MaxFields)
            errors.Add($"The form cannot have more than {MaxFields} fields.");

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            var key = field.Key ?? string.Empty;
            if (!KeyPattern().IsMatch(key))
                errors.Add($"Field key '{key}' must start with a lowercase letter and contain only letters and digits (max 40).");
            else if (!keys.Add(key))
                errors.Add($"Field key '{key}' is duplicated.");

            if (string.IsNullOrWhiteSpace(field.LabelEs) || string.IsNullOrWhiteSpace(field.LabelEn))
                errors.Add($"Field '{key}' requires the Spanish and English labels.");

            if (!ServiceInputFieldTypes.All.Contains(field.Type))
            {
                errors.Add($"Field '{key}' has an unknown type '{field.Type}'.");
                continue;
            }

            if (field.Type == ServiceInputFieldTypes.Select)
            {
                var options = field.Options ?? [];
                if (options.Count == 0)
                    errors.Add($"Select field '{key}' requires options.");
                if (options.Any(o => string.IsNullOrWhiteSpace(o.Value)))
                    errors.Add($"Select field '{key}' has an option without value.");
                if (options.Select(o => o.Value).Distinct(StringComparer.Ordinal).Count() != options.Count)
                    errors.Add($"Select field '{key}' has duplicated option values.");
            }
            else if (field.Options is { Count: > 0 })
            {
                errors.Add($"Only select fields can have options ('{key}').");
            }

            if (field.Min is not null && field.Max is not null && field.Min > field.Max)
                errors.Add($"Field '{key}' has min greater than max.");

            if (field.MaxLength is not null && field.MaxLength <= 0)
                errors.Add($"Field '{key}' must have a positive maxLength.");
        }

        if (fields.Count(f => f.Type == ServiceInputFieldTypes.Containers) > 1)
            errors.Add("The form can have at most one containers field.");

        return errors;
    }

    /// <summary>
    /// Valida los datos ingresados contra el formulario. Claves desconocidas y tipos incorrectos siempre son
    /// error; con <paramref name="requireComplete"/> (al enviar) se exigen además los obligatorios. Los campos
    /// <c>file</c> se cumplen con un adjunto de esa clave (<paramref name="attachedFileKeys"/>) y
    /// <c>containers</c> solo acepta contenedores del BL (<paramref name="blContainers"/>).
    /// </summary>
    public static ServiceInputValidation ValidateValues(
        IReadOnlyList<ServiceInputField> fields,
        string? valuesJson,
        IReadOnlyCollection<string> blContainers,
        IReadOnlyCollection<string> attachedFileKeys,
        bool requireComplete)
    {
        var errors = new List<ServiceInputError>();
        var normalized = new JsonObject();
        var containers = new List<string>();
        var numbers = new Dictionary<string, decimal>(StringComparer.Ordinal);

        JsonObject? values;
        try
        {
            values = string.IsNullOrWhiteSpace(valuesJson) ? [] : JsonNode.Parse(valuesJson) as JsonObject;
        }
        catch (JsonException)
        {
            values = null;
        }

        if (values is null)
        {
            errors.Add(new ServiceInputError("inputValues", "The input values must be a JSON object."));
            return new ServiceInputValidation(errors, "{}", containers, numbers);
        }

        var byKey = fields.ToDictionary(f => f.Key, StringComparer.Ordinal);
        foreach (var (key, _) in values)
        {
            if (!byKey.TryGetValue(key, out var field))
                errors.Add(new ServiceInputError(key, $"Unknown field '{key}'."));
            else if (field.Type == ServiceInputFieldTypes.File)
                errors.Add(new ServiceInputError(key, "File fields are answered by uploading an attachment."));
        }

        var blSet = new HashSet<string>(blContainers.Select(c => c.Trim().ToUpperInvariant()), StringComparer.Ordinal);

        foreach (var field in fields)
        {
            if (field.Type == ServiceInputFieldTypes.File)
            {
                if (requireComplete && field.Required && !attachedFileKeys.Contains(field.Key))
                    errors.Add(new ServiceInputError(field.Key, $"'{field.LabelEn}' requires an attached file."));
                continue;
            }

            var node = values[field.Key];
            if (node is null || IsEmpty(node))
            {
                if (requireComplete && field.Required)
                    errors.Add(new ServiceInputError(field.Key, $"'{field.LabelEn}' is required."));
                continue;
            }

            var error = field.Type switch
            {
                ServiceInputFieldTypes.Text or ServiceInputFieldTypes.TextArea => Text(field, node, normalized),
                ServiceInputFieldTypes.Date => Date(field, node, normalized),
                ServiceInputFieldTypes.Number => Number(field, node, normalized, numbers),
                ServiceInputFieldTypes.Select => Select(field, node, normalized),
                ServiceInputFieldTypes.Containers => Containers(field, node, normalized, blSet, containers),
                _ => null
            };

            if (error is not null)
                errors.Add(new ServiceInputError(field.Key, error));
        }

        return new ServiceInputValidation(errors, normalized.ToJsonString(), containers, numbers);
    }

    private static bool IsEmpty(JsonNode node) => node switch
    {
        JsonValue value when value.TryGetValue<string>(out var text) => string.IsNullOrWhiteSpace(text),
        JsonArray array => array.Count == 0,
        _ => false
    };

    private static string? Text(ServiceInputField field, JsonNode node, JsonObject normalized)
    {
        if (node is not JsonValue value || !value.TryGetValue<string>(out var text))
            return "Must be text.";

        var max = field.MaxLength ?? (field.Type == ServiceInputFieldTypes.TextArea ? DefaultTextAreaLength : DefaultTextLength);
        text = text.Trim();
        if (text.Length > max)
            return $"Must not exceed {max} characters.";

        normalized[field.Key] = text;
        return null;
    }

    private static string? Date(ServiceInputField field, JsonNode node, JsonObject normalized)
    {
        if (node is not JsonValue value
            || !value.TryGetValue<string>(out var text)
            || !DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return "Must be a date (yyyy-MM-dd).";

        normalized[field.Key] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return null;
    }

    private static string? Number(ServiceInputField field, JsonNode node, JsonObject normalized, Dictionary<string, decimal> numbers)
    {
        if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.Number || !value.TryGetValue<decimal>(out var number))
            return "Must be a number.";

        if (field.Integer && number != decimal.Truncate(number))
            return "Must be a whole number.";
        if (field.Min is not null && number < field.Min)
            return $"Must be greater than or equal to {field.Min.Value.ToString(CultureInfo.InvariantCulture)}.";
        if (field.Max is not null && number > field.Max)
            return $"Must be less than or equal to {field.Max.Value.ToString(CultureInfo.InvariantCulture)}.";

        normalized[field.Key] = number;
        numbers[field.Key] = number;
        return null;
    }

    private static string? Select(ServiceInputField field, JsonNode node, JsonObject normalized)
    {
        if (node is not JsonValue value || !value.TryGetValue<string>(out var text))
            return "Must be one of the options.";

        var option = (field.Options ?? []).FirstOrDefault(o => string.Equals(o.Value, text.Trim(), StringComparison.Ordinal));
        if (option is null)
            return "Must be one of the options.";

        normalized[field.Key] = option.Value;
        return null;
    }

    private static string? Containers(
        ServiceInputField field,
        JsonNode node,
        JsonObject normalized,
        HashSet<string> blContainers,
        List<string> selected)
    {
        if (node is not JsonArray array)
            return "Must be a list of containers.";

        var numbers = new List<string>();
        foreach (var item in array)
        {
            if (item is not JsonValue value || !value.TryGetValue<string>(out var text) || string.IsNullOrWhiteSpace(text))
                return "Must be a list of containers.";

            var number = text.Trim().ToUpperInvariant();
            if (!blContainers.Contains(number))
                return $"The container '{number}' does not belong to the shipment.";
            if (!numbers.Contains(number))
                numbers.Add(number);
        }

        selected.AddRange(numbers);
        normalized[field.Key] = new JsonArray(numbers.Select(n => (JsonNode)JsonValue.Create(n)!).ToArray());
        return null;
    }
}
