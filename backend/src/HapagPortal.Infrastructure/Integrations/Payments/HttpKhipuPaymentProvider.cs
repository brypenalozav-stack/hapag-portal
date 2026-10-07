using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>Opciones de Khipu (<c>Integrations:Khipu</c>).</summary>
public sealed class KhipuOptions
{
    /// <summary>Minutos de vigencia del cobro (<c>expires_date</c>); 0 = sin vencimiento propio (el de la cuenta).</summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>Diferencia máxima entre la hora de la firma (<c>t</c>) y la del servidor (protección contra repetición).</summary>
    public int WebhookToleranceSeconds { get; set; } = 300;

    /// <summary>Envía el RUT del pagador como <c>fixed_payer_personal_identifier</c> (solo ese RUT podrá pagar).</summary>
    public bool FixPayerTaxId { get; set; }
}

/// <summary>
/// Khipu, API v3 (https://payment-api.khipu.com, cabecera <c>x-api-key</c> con el secreto <c>KHIPU_SECRET</c>):
/// <list type="bullet">
/// <item>Crear: <c>POST /v3/payments</c> con <c>transaction_id</c> = referencia del portal y <c>notify_api_version</c> 3.0.</item>
/// <item>Consultar: <c>GET /v3/payments/{payment_id}</c>; anular un cobro pendiente: <c>DELETE /v3/payments/{payment_id}</c>.</item>
/// <item>Notificación v3: <c>x-khipu-signature: t=&lt;ms&gt;,s=&lt;base64&gt;</c>, con
/// <c>s = Base64(HMAC-SHA256(KHIPU_WEBHOOK_SECRET, t + "." + cuerpoCrudo))</c>, comparada en tiempo constante y con
/// <c>t</c> dentro de la tolerancia. Luego el estado se consulta siempre con el GET.</item>
/// </list>
/// </summary>
public sealed class HttpKhipuPaymentProvider(
    HttpClient httpClient,
    ISecretResolver secretResolver,
    KhipuOptions options,
    PaymentPublicUrls publicUrls,
    TimeProvider timeProvider,
    ILogger<HttpKhipuPaymentProvider> logger) : IPaymentProvider
{
    public const string SignatureHeader = "x-khipu-signature";
    private const string ApiKeyHeader = "x-api-key";
    private const string System = IntegrationSystems.Khipu;

    private static readonly HashSet<string> FailedStatusDetails =
        new(["rejected-by-payer", "marked-as-abuse", "reversed"], StringComparer.OrdinalIgnoreCase);

    public string ProviderCode => IntegrationSystems.Khipu;

    public bool VerifiesNotifications => true;

    public async Task<Result<PaymentInitiation>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = await ApiKeyAsync(cancellationToken);
        if (apiKey.IsFailure)
            return Result<PaymentInitiation>.Failure(apiKey.Error);

        var returnUrl = publicUrls.Site(request.ReturnUrl);
        var notifyUrl = publicUrls.Api(request.NotifyUrl);
        if (returnUrl.IsFailure || notifyUrl.IsFailure)
            return Result<PaymentInitiation>.Failure(returnUrl.IsFailure ? returnUrl.Error : notifyUrl.Error);

        var body = new Dictionary<string, object?>
        {
            ["amount"] = GatewayFormat.Amount(request.Amount, request.Currency),
            ["currency"] = request.Currency.ToUpperInvariant(),
            ["subject"] = GatewayFormat.Truncate(request.Subject, 255),
            ["transaction_id"] = request.ExternalReference,
            ["return_url"] = returnUrl.Value,
            ["cancel_url"] = returnUrl.Value,
            ["notify_url"] = notifyUrl.Value,
            ["notify_api_version"] = "3.0",
        };

        if (options.ExpirationMinutes > 0)
        {
            body["expires_date"] = timeProvider.GetUtcNow().AddMinutes(options.ExpirationMinutes)
                .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(request.PayerEmail))
            body["payer_email"] = request.PayerEmail;
        if (!string.IsNullOrWhiteSpace(request.PayerName))
            body["payer_name"] = request.PayerName;
        if (options.FixPayerTaxId && !string.IsNullOrWhiteSpace(request.PayerTaxId))
            body["fixed_payer_personal_identifier"] = request.PayerTaxId;

        var response = await GatewayHttp.SendAsync(
            httpClient, System, "createPayment", HttpMethod.Post, "v3/payments", cancellationToken, body, headers: Auth(apiKey.Value));
        if (response.IsFailure)
            return Result<PaymentInitiation>.Failure(response.Error);

        var paymentId = GatewayJson.Text(response.Value, "payment_id");
        var paymentUrl = GatewayJson.Text(response.Value, "payment_url");
        if (paymentId is null || paymentUrl is null)
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.InvalidResponse(System));

        return Result<PaymentInitiation>.Success(
            new PaymentInitiation(paymentId, request.ExternalReference, PaymentStatus.Pending, paymentUrl));
    }

    public async Task<Result<PaymentVerification>> GetStatusAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
            return Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(System));

        var apiKey = await ApiKeyAsync(cancellationToken);
        if (apiKey.IsFailure)
            return Result<PaymentVerification>.Failure(apiKey.Error);

        var response = await GatewayHttp.SendAsync(
            httpClient, System, "getPaymentById", HttpMethod.Get, $"v3/payments/{Uri.EscapeDataString(request.ProviderReference)}",
            cancellationToken, headers: Auth(apiKey.Value));
        if (response.IsFailure)
            return Result<PaymentVerification>.Failure(response.Error);

        var payment = response.Value;
        var reference = GatewayJson.Text(payment, "transaction_id");
        var amount = GatewayJson.Decimal(payment, "amount");
        var currency = GatewayJson.Text(payment, "currency");
        var status = GatewayJson.Text(payment, "status");
        if (reference is null || amount is null || currency is null || status is null)
            return Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(System));

        return Result<PaymentVerification>.Success(new PaymentVerification(
            reference, MapStatus(status, GatewayJson.Text(payment, "status_detail")), amount.Value, currency,
            GatewayJson.Text(payment, "payment_id")));
    }

    public async Task<Result> CancelAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
            return Result.Success();

        var apiKey = await ApiKeyAsync(cancellationToken);
        if (apiKey.IsFailure)
            return Result.Failure(apiKey.Error);

        var response = await GatewayHttp.SendAsync(
            httpClient, System, "deletePaymentById", HttpMethod.Delete, $"v3/payments/{Uri.EscapeDataString(request.ProviderReference)}",
            cancellationToken, headers: Auth(apiKey.Value));
        return response.IsSuccess ? Result.Success() : Result.Failure(response.Error);
    }

    public async Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        var secret = await secretResolver.ResolveAsync(SecretTypes.KhipuWebhookSecret, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogWarning("Notificación de Khipu rechazada: falta el secreto {SecretType}", SecretTypes.KhipuWebhookSecret);
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
        }

        var header = GatewayFormat.Header(notification.Headers, SignatureHeader);
        if (!KhipuSignature.Verify(header, notification.RawBody, secret, timeProvider.GetUtcNow(), TimeSpan.FromSeconds(Math.Max(1, options.WebhookToleranceSeconds))))
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);

        try
        {
            using var document = JsonDocument.Parse(notification.RawBody);
            var root = document.RootElement;
            var paymentId = GatewayJson.Text(root, "payment_id");
            var reference = GatewayJson.Text(root, "transaction_id");

            return paymentId is null && reference is null
                ? Result<PaymentNotificationInfo>.Failure(Error.Unauthorized)
                : Result<PaymentNotificationInfo>.Success(new PaymentNotificationInfo(paymentId, reference));
        }
        catch (JsonException)
        {
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
        }
    }

    /// <summary>
    /// done es pagado salvo que status_detail lo anule (reversed, rejected-by-payer, marked-as-abuse); verifying es en
    /// proceso; pending (y cualquier otro) es pendiente.
    /// </summary>
    public static string MapStatus(string status, string? statusDetail)
    {
        if (statusDetail is not null && FailedStatusDetails.Contains(statusDetail))
            return PaymentStatus.Failed;

        return status.ToLowerInvariant() switch
        {
            "done" => PaymentStatus.Confirmed,
            "verifying" => PaymentStatus.Processing,
            _ => PaymentStatus.Pending,
        };
    }

    private async Task<Result<string>> ApiKeyAsync(CancellationToken cancellationToken)
    {
        var apiKey = await secretResolver.ResolveAsync(SecretTypes.KhipuSecret, null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(apiKey))
            return Result<string>.Success(apiKey);

        logger.LogWarning("Khipu sin configurar: falta el secreto {SecretType}", SecretTypes.KhipuSecret);
        return Result<string>.Failure(DomainErrors.Integration.NotConfigured(System));
    }

    private static KeyValuePair<string, string>[] Auth(string apiKey) => [new(ApiKeyHeader, apiKey)];
}

/// <summary>Firma de las notificaciones v3 de Khipu (<c>x-khipu-signature: t=...,s=...</c>).</summary>
public static class KhipuSignature
{
    /// <summary><c>Base64(HMAC-SHA256(secret, t + "." + cuerpoCrudo))</c>.</summary>
    public static string Compute(string timestamp, string rawBody, string secret) =>
        Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(timestamp + "." + rawBody)));

    /// <summary>Firma válida y hecha dentro de la tolerancia. Fail-closed ante cualquier formato inesperado.</summary>
    public static bool Verify(string? header, string rawBody, string secret, DateTimeOffset now, TimeSpan tolerance)
    {
        if (string.IsNullOrWhiteSpace(header))
            return false;

        string? timestamp = null;
        string? signature = null;
        foreach (var part in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                continue;

            var name = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            if (name == "t")
                timestamp = value;
            else if (name == "s")
                signature = value;
        }

        if (timestamp is null || signature is null ||
            !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return false;
        }

        DateTimeOffset signedAt;
        try
        {
            signedAt = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if ((now - signedAt).Duration() > tolerance)
            return false;

        byte[] provided;
        try
        {
            provided = Convert.FromBase64String(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(timestamp + "." + rawBody));
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
