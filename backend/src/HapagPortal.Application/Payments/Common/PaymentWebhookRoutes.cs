namespace HapagPortal.Application.Payments.Common;

using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;

/// <summary>
/// Rutas de notificación de cada pasarela (<c>POST /api/v1/payments/webhook/{pasarela}</c>). Son las URL que se
/// registran en cada pasarela o que se envían al crear el cobro (ver docs/integraciones/pasarelas-pago.md).
/// </summary>
public static class PaymentWebhookRoutes
{
    public const string BasePath = "/api/v1/payments/webhook/";

    private static readonly Dictionary<string, string> SegmentsByProvider = new(StringComparer.OrdinalIgnoreCase)
    {
        [PaymentProviderKeys.Khipu] = "khipu",
        [PaymentProviderKeys.BancoChile] = "banco-chile",
        [PaymentProviderKeys.Santander] = "getnet",
        [PaymentProviderKeys.Bci] = "bci",
    };

    /// <summary>Ruta relativa de la notificación de la pasarela (para la clave sin ruta propia, su clave en minúsculas).</summary>
    public static string PathFor(string providerKey) =>
        BasePath + (SegmentsByProvider.TryGetValue(providerKey, out var segment) ? segment : providerKey.ToLowerInvariant());

    /// <summary>Clave de la pasarela a partir del último segmento de la ruta, o nulo si no corresponde a ninguna.</summary>
    public static string? ProviderFor(string segment) =>
        SegmentsByProvider.FirstOrDefault(kv => string.Equals(kv.Value, segment, StringComparison.OrdinalIgnoreCase)).Key;
}

/// <summary>Serialización del formulario firmado de la pasarela, que se guarda con el pago para repetir la redirección.</summary>
public static class PaymentRedirectForms
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string? Serialize(PaymentRedirectForm? form) =>
        form is null ? null : JsonSerializer.Serialize(new RedirectFormDto(form.Method, form.Action, form.Fields), Json);

    public static RedirectFormDto? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<RedirectFormDto>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
