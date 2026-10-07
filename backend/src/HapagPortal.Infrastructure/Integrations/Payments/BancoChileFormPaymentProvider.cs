using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Opciones del botón de Banco de Chile (<c>Integrations:BancoChile</c>). El protocolo NO es público: todos los valores
/// salen del manual técnico que el banco entrega con el contrato. Los nombres por defecto son solo el patrón típico de
/// los botones bancarios chilenos y deben reemplazarse por los del manual.
/// </summary>
public sealed class BancoChileOptions
{
    /// <summary>URL del ambiente seguro del banco a la que el navegador envía el formulario. Vacía = sin configurar.</summary>
    public string FormUrl { get; set; } = string.Empty;

    public string FormMethod { get; set; } = "POST";

    // Nombres de los campos del formulario de inicio. Uno vacío no se envía.
    public string MerchantIdField { get; set; } = "convenio";
    public string OrderField { get; set; } = "orden";
    public string AmountField { get; set; } = "monto";
    public string ReturnUrlField { get; set; } = "urlRetorno";
    public string NotifyUrlField { get; set; } = "urlNotificacion";
    public string CancelUrlField { get; set; } = "urlCancelacion";
    public string DateField { get; set; } = "fecha";
    public string DateFormat { get; set; } = "yyyyMMddHHmmss";
    public string SignatureField { get; set; } = "firma";

    /// <summary>Campos fijos adicionales que pida el manual (por ejemplo, la versión del protocolo).</summary>
    public Dictionary<string, string> ExtraFields { get; set; } = [];

    /// <summary>Campos del formulario que se firman, en el orden del manual. Vacío = sin configurar.</summary>
    public string[] SignedFields { get; set; } = [];

    /// <summary>Separador entre los valores firmados ("" = concatenados).</summary>
    public string SignatureSeparator { get; set; } = string.Empty;

    /// <summary><c>HMACSHA256</c>, <c>HMACSHA1</c> o <c>SHA256</c> (hash de los valores con la llave concatenada al final).</summary>
    public string SignatureAlgorithm { get; set; } = "HMACSHA256";

    /// <summary><c>Hex</c> (minúsculas), <c>HexUpper</c> o <c>Base64</c>.</summary>
    public string SignatureEncoding { get; set; } = "Hex";

    // Notificación del banco (servidor a servidor).
    public string NotificationOrderField { get; set; } = "orden";
    public string NotificationAmountField { get; set; } = "monto";
    public string NotificationStatusField { get; set; } = "estado";
    public string NotificationTransactionField { get; set; } = "idTransaccion";
    public string NotificationSignatureField { get; set; } = "firma";

    /// <summary>Campos de la notificación que se firman, en orden. Vacío = se usan los mismos de <see cref="SignedFields"/>.</summary>
    public string[] NotificationSignedFields { get; set; } = [];

    /// <summary>Valores del estado que significan pagado y rechazado (comparación exacta, sin distinguir mayúsculas).</summary>
    public string[] PaidStatusValues { get; set; } = [];
    public string[] FailedStatusValues { get; set; } = [];

    /// <summary>Texto que el banco espera como respuesta a la notificación (por ejemplo "OK" o "ACEPTADO").</summary>
    public string NotificationAcknowledgement { get; set; } = "OK";
}

/// <summary>
/// Botón de Banco de Chile ("Pagos Electrónicos en Otros Sitios"): adaptador configurable de formulario firmado.
/// <list type="bullet">
/// <item>Inicio: arma un formulario con convenio (<c>BANCOCHILE_MERCHANT_ID</c>), orden (referencia del portal), monto
/// CLP, URL de retorno, notificación y cancelación, fecha y firma (<see cref="SignedForm"/>, llave
/// <c>BANCOCHILE_SIGNING_KEY</c>). El navegador lo envía al banco.</item>
/// <item>Notificación: verifica la firma con los campos y el orden configurados; el estado firmado se compara con el
/// pago (referencia, monto, CLP) igual que una consulta.</item>
/// <item>Consulta de estado: el manual no está disponible; queda sin configurar (<c>Integration.NotConfigured</c>) y la
/// conciliación omite estos pagos.</item>
/// </list>
/// DESHABILITADO hasta recibir el manual: sin <c>FormUrl</c>, <c>SignedFields</c>, estados ni secretos, el cobro falla con
/// <c>Integration.NotConfigured</c> (el arranque no falla).
/// </summary>
public sealed class BancoChileFormPaymentProvider(
    ISecretResolver secretResolver,
    BancoChileOptions options,
    PaymentPublicUrls publicUrls,
    TimeProvider timeProvider,
    ILogger<BancoChileFormPaymentProvider> logger) : IPaymentProvider
{
    private const string System = IntegrationSystems.BancoChile;
    private static readonly TimeZoneInfo ChileTime = ResolveChileTime();

    public string ProviderCode => PaymentProviderKeys.BancoChile;

    public bool VerifiesNotifications => true;

    public async Task<Result<PaymentInitiation>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
    {
        var merchantId = await secretResolver.ResolveAsync(SecretTypes.BancoChileMerchantId, null, cancellationToken);
        var signingKey = await secretResolver.ResolveAsync(SecretTypes.BancoChileSigningKey, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(signingKey) ||
            !Uri.TryCreate(options.FormUrl, UriKind.Absolute, out _) || options.SignedFields.Length == 0 ||
            string.IsNullOrWhiteSpace(options.SignatureField))
        {
            logger.LogWarning("Botón de Banco de Chile sin configurar: falta el manual del banco (FormUrl, SignedFields o secretos)");
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.NotConfigured(System));
        }

        if (!string.Equals(request.Currency, "CLP", StringComparison.OrdinalIgnoreCase))
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.InvalidResponse(System));

        var returnUrl = publicUrls.Site(request.ReturnUrl);
        var notifyUrl = publicUrls.Api(request.NotifyUrl);
        if (returnUrl.IsFailure || notifyUrl.IsFailure)
            return Result<PaymentInitiation>.Failure(returnUrl.IsFailure ? returnUrl.Error : notifyUrl.Error);

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in options.ExtraFields)
            fields[key] = value;
        Add(fields, options.MerchantIdField, merchantId);
        Add(fields, options.OrderField, request.ExternalReference);
        Add(fields, options.AmountField, GatewayFormat.AmountText(request.Amount, "CLP"));
        Add(fields, options.ReturnUrlField, returnUrl.Value);
        Add(fields, options.NotifyUrlField, notifyUrl.Value);
        Add(fields, options.CancelUrlField, returnUrl.Value);
        Add(fields, options.DateField, TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), ChileTime).ToString(options.DateFormat, CultureInfo.InvariantCulture));

        var signature = SignedForm.Sign(fields, options.SignedFields, options.SignatureSeparator, signingKey, options.SignatureAlgorithm, options.SignatureEncoding);
        if (signature is null)
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.NotConfigured(System));
        fields[options.SignatureField] = signature;

        var form = new PaymentRedirectForm(options.FormMethod.ToUpperInvariant(), options.FormUrl, fields);
        return Result<PaymentInitiation>.Success(
            new PaymentInitiation(request.ExternalReference, request.ExternalReference, PaymentStatus.Pending, options.FormUrl, form));
    }

    /// <summary>Sin manual no hay consulta de estado: la confirmación llega solo por la notificación firmada.</summary>
    public Task<Result<PaymentVerification>> GetStatusAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<PaymentVerification>.Failure(DomainErrors.Integration.NotConfigured(System)));

    public Task<Result> CancelAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    public async Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        var signingKey = await secretResolver.ResolveAsync(SecretTypes.BancoChileSigningKey, null, cancellationToken);
        var signedFields = options.NotificationSignedFields.Length > 0 ? options.NotificationSignedFields : options.SignedFields;
        if (string.IsNullOrWhiteSpace(signingKey) || signedFields.Length == 0)
        {
            logger.LogWarning("Notificación de Banco de Chile rechazada: el botón no está configurado");
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
        }

        var fields = ParseFields(notification.RawBody);
        if (fields is null || !fields.TryGetValue(options.NotificationSignatureField, out var provided))
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);

        var expected = SignedForm.Sign(fields, signedFields, options.SignatureSeparator, signingKey, options.SignatureAlgorithm, options.SignatureEncoding);
        var matches = expected is not null && (options.SignatureEncoding.Equals("Base64", StringComparison.OrdinalIgnoreCase)
            ? GatewayFormat.FixedTimeEquals(expected, provided.Trim())
            : GatewayFormat.FixedTimeEquals(expected.ToLowerInvariant(), provided.Trim().ToLowerInvariant()));
        if (!matches)
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);

        var order = fields.GetValueOrDefault(options.NotificationOrderField);
        var amountText = fields.GetValueOrDefault(options.NotificationAmountField);
        var status = fields.GetValueOrDefault(options.NotificationStatusField) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(order) ||
            !decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
        }

        var mapped = options.PaidStatusValues.Contains(status, StringComparer.OrdinalIgnoreCase) ? PaymentStatus.Confirmed
            : options.FailedStatusValues.Contains(status, StringComparer.OrdinalIgnoreCase) ? PaymentStatus.Failed
            : PaymentStatus.Pending;

        var signedStatus = new PaymentVerification(order, mapped, amount, "CLP", fields.GetValueOrDefault(options.NotificationTransactionField));
        return Result<PaymentNotificationInfo>.Success(
            new PaymentNotificationInfo(null, order, options.NotificationAcknowledgement, signedStatus));
    }

    private static void Add(Dictionary<string, string> fields, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(name))
            fields[name] = value;
    }

    private static Dictionary<string, string>? ParseFields(string rawBody)
    {
        var body = rawBody.Trim();
        if (body.Length == 0)
            return null;

        if (!body.StartsWith('{'))
            return QueryHelpers.ParseQuery(body).ToDictionary(kv => kv.Key, kv => kv.Value.ToString(), StringComparer.Ordinal);

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.EnumerateObject()
                .Where(p => p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                .ToDictionary(
                    p => p.Name,
                    p => p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.GetRawText(),
                    StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TimeZoneInfo ResolveChileTime()
    {
        foreach (var id in new[] { "America/Santiago", "Pacific SA Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}

/// <summary>Firma configurable de los formularios de los botones bancarios.</summary>
public static class SignedForm
{
    /// <summary>
    /// Firma los valores de <paramref name="signedFields"/> (en ese orden, unidos por <paramref name="separator"/>).
    /// Devuelve nulo si falta un campo o el algoritmo o la codificación no se reconocen.
    /// </summary>
    public static string? Sign(
        IReadOnlyDictionary<string, string> fields,
        IReadOnlyList<string> signedFields,
        string separator,
        string key,
        string algorithm,
        string encoding)
    {
        var values = new List<string>(signedFields.Count);
        foreach (var name in signedFields)
        {
            if (!fields.TryGetValue(name, out var value))
                return null;
            values.Add(value);
        }

        var data = Encoding.UTF8.GetBytes(string.Join(separator, values));
        var keyBytes = Encoding.UTF8.GetBytes(key);

#pragma warning disable CA5350 // HMAC-SHA1: solo si el manual del banco lo exige.
        byte[]? hash = algorithm.ToUpperInvariant() switch
        {
            "HMACSHA256" => HMACSHA256.HashData(keyBytes, data),
            "HMACSHA1" => HMACSHA1.HashData(keyBytes, data),
            "SHA256" => SHA256.HashData([.. data, .. keyBytes]),
            _ => null
        };
#pragma warning restore CA5350

        if (hash is null)
            return null;

        return encoding.ToUpperInvariant() switch
        {
            "HEX" => Convert.ToHexStringLower(hash),
            "HEXUPPER" => Convert.ToHexString(hash),
            "BASE64" => Convert.ToBase64String(hash),
            _ => null
        };
    }
}
