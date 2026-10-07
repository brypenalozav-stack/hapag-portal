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

/// <summary>Opciones de Getnet Web Checkout (<c>Integrations:Getnet</c>).</summary>
public sealed class GetnetOptions
{
    public string Locale { get; set; } = "es_CL";

    /// <summary>Minutos de vigencia de la sesión (mínimo 5, exigencia de Getnet).</summary>
    public int ExpirationMinutes { get; set; } = 30;

    /// <summary>IP y User-Agent que se informan cuando no se conocen los del pagador (Getnet los exige).</summary>
    public string FallbackIpAddress { get; set; } = "127.0.0.1";

    public string FallbackUserAgent { get; set; } = "HapagPortal/1.0";
}

/// <summary>
/// Botón Santander = Getnet Chile Web Checkout (API de PlacetoPay; pruebas https://checkout.test.getnet.cl, producción
/// https://checkout.getnet.cl). Credenciales <c>GETNET_LOGIN</c> y <c>GETNET_SECRET_KEY</c>. Cada llamada lleva el
/// objeto <c>auth</c> (<see cref="GetnetAuth"/>):
/// <list type="bullet">
/// <item>Crear sesión: <c>POST /api/session</c> → <c>requestId</c> y <c>processUrl</c> (adonde va el pagador).</item>
/// <item>Consultar: <c>POST /api/session/{requestId}</c>; anular: <c>POST /api/session/{requestId}/cancel</c>.</item>
/// <item>Notificación (URL registrada en el panel de Getnet, se envía una sola vez): firma
/// <c>"sha256:" + hex(SHA-256(requestId + status + date + secretKey))</c> o, en el formato antiguo, hex(SHA-1(...)).
/// Siempre se vuelve a consultar la sesión.</item>
/// </list>
/// </summary>
public sealed class GetnetPaymentProvider(
    HttpClient httpClient,
    ISecretResolver secretResolver,
    GetnetOptions options,
    PaymentPublicUrls publicUrls,
    TimeProvider timeProvider,
    ILogger<GetnetPaymentProvider> logger) : IPaymentProvider
{
    private const string System = IntegrationSystems.Getnet;

    public string ProviderCode => PaymentProviderKeys.Santander;

    public bool VerifiesNotifications => true;

    public async Task<Result<PaymentInitiation>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
    {
        var credentials = await CredentialsAsync(cancellationToken);
        if (credentials.IsFailure)
            return Result<PaymentInitiation>.Failure(credentials.Error);

        var returnUrl = publicUrls.Site(request.ReturnUrl);
        if (returnUrl.IsFailure)
            return Result<PaymentInitiation>.Failure(returnUrl.Error);

        var now = timeProvider.GetUtcNow();
        var body = new Dictionary<string, object?>
        {
            ["auth"] = GetnetAuth.Create(credentials.Value.Login, credentials.Value.SecretKey, now),
            ["locale"] = options.Locale,
            ["payment"] = new Dictionary<string, object?>
            {
                ["reference"] = request.ExternalReference,
                ["description"] = GatewayFormat.Truncate(request.Subject, 250),
                ["amount"] = new Dictionary<string, object?>
                {
                    ["currency"] = request.Currency.ToUpperInvariant(),
                    ["total"] = GatewayFormat.Amount(request.Amount, request.Currency),
                },
            },
            ["expiration"] = GetnetAuth.IsoDate(now.AddMinutes(Math.Max(5, options.ExpirationMinutes))),
            ["returnUrl"] = returnUrl.Value,
            ["cancelUrl"] = returnUrl.Value,
            ["ipAddress"] = string.IsNullOrWhiteSpace(request.ClientIpAddress) ? options.FallbackIpAddress : request.ClientIpAddress,
            ["userAgent"] = string.IsNullOrWhiteSpace(request.ClientUserAgent) ? options.FallbackUserAgent : request.ClientUserAgent,
        };

        if (!string.IsNullOrWhiteSpace(request.PayerEmail))
        {
            body["buyer"] = new Dictionary<string, object?>
            {
                ["email"] = request.PayerEmail,
                ["name"] = request.PayerName,
            };
        }

        var response = await GatewayHttp.SendAsync(httpClient, System, "createSession", HttpMethod.Post, "api/session", cancellationToken, body);
        if (response.IsFailure)
            return Result<PaymentInitiation>.Failure(response.Error);

        var requestId = GatewayJson.Text(response.Value, "requestId");
        var processUrl = GatewayJson.Text(response.Value, "processUrl");
        if (!IsOk(response.Value) || requestId is null || processUrl is null)
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.InvalidResponse(System));

        return Result<PaymentInitiation>.Success(new PaymentInitiation(requestId, request.ExternalReference, PaymentStatus.Pending, processUrl));
    }

    public async Task<Result<PaymentVerification>> GetStatusAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
            return Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(System));

        var credentials = await CredentialsAsync(cancellationToken);
        if (credentials.IsFailure)
            return Result<PaymentVerification>.Failure(credentials.Error);

        var body = new Dictionary<string, object?>
        {
            ["auth"] = GetnetAuth.Create(credentials.Value.Login, credentials.Value.SecretKey, timeProvider.GetUtcNow()),
        };
        var response = await GatewayHttp.SendAsync(
            httpClient, System, "getRequestInformation", HttpMethod.Post, $"api/session/{Uri.EscapeDataString(request.ProviderReference)}",
            cancellationToken, body);
        if (response.IsFailure)
            return Result<PaymentVerification>.Failure(response.Error);

        return Map(response.Value, request) is { } verification
            ? Result<PaymentVerification>.Success(verification)
            : Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(System));
    }

    public async Task<Result> CancelAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
            return Result.Success();

        var credentials = await CredentialsAsync(cancellationToken);
        if (credentials.IsFailure)
            return Result.Failure(credentials.Error);

        var body = new Dictionary<string, object?>
        {
            ["auth"] = GetnetAuth.Create(credentials.Value.Login, credentials.Value.SecretKey, timeProvider.GetUtcNow()),
        };
        var response = await GatewayHttp.SendAsync(
            httpClient, System, "cancelSession", HttpMethod.Post, $"api/session/{Uri.EscapeDataString(request.ProviderReference)}/cancel",
            cancellationToken, body);
        if (response.IsFailure)
            return Result.Failure(response.Error);

        return IsOk(response.Value) ? Result.Success() : Result.Failure(DomainErrors.Integration.InvalidResponse(System));
    }

    public async Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        var secretKey = await secretResolver.ResolveAsync(SecretTypes.GetnetSecretKey, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            logger.LogWarning("Notificación de Getnet rechazada: falta el secreto {SecretType}", SecretTypes.GetnetSecretKey);
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
        }

        try
        {
            using var document = JsonDocument.Parse(notification.RawBody);
            var root = document.RootElement;
            var requestId = GatewayJson.Text(root, "requestId");
            var status = GatewayJson.Text(root, "status.status");
            var date = GatewayJson.Text(root, "status.date");
            var signature = GatewayJson.Text(root, "signature");

            if (requestId is null || status is null || date is null ||
                !GetnetAuth.VerifyNotificationSignature(signature, requestId, status, date, secretKey))
            {
                return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
            }

            return Result<PaymentNotificationInfo>.Success(new PaymentNotificationInfo(requestId, GatewayJson.Text(root, "reference")));
        }
        catch (JsonException)
        {
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
        }
    }

    /// <summary>
    /// APPROVED es pagado; REJECTED y PARTIAL_EXPIRED, fallido; PENDING, APPROVED_PARTIAL y cualquier otro, pendiente
    /// (un pago parcial nunca se aprueba).
    /// </summary>
    public static string MapStatus(string status) => status.ToUpperInvariant() switch
    {
        "APPROVED" => PaymentStatus.Confirmed,
        "REJECTED" or "PARTIAL_EXPIRED" => PaymentStatus.Failed,
        _ => PaymentStatus.Pending,
    };

    private static PaymentVerification? Map(JsonElement root, PaymentStatusRequest request)
    {
        var status = GatewayJson.Text(root, "status.status");
        if (status is null || string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase))
            return null;

        var mapped = MapStatus(status);

        // Para un pago aprobado, monto y moneda del intento aprobado; si no, los de la sesión.
        JsonElement? approved = null;
        if (GatewayJson.Find(root, "payment") is { ValueKind: JsonValueKind.Array } attempts)
        {
            foreach (var attempt in attempts.EnumerateArray())
            {
                if (string.Equals(GatewayJson.Text(attempt, "status.status"), "APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    approved = attempt;
                    break;
                }
            }
        }

        var reference = GatewayJson.Text(root, "request.payment.reference")
            ?? (approved is { } a ? GatewayJson.Text(a, "reference") : null)
            ?? request.ExternalReference;
        decimal? amount = approved is { } p ? GatewayJson.Decimal(p, "amount.from.total") : null;
        var currency = approved is { } c ? GatewayJson.Text(c, "amount.from.currency") : null;
        amount ??= GatewayJson.Decimal(root, "request.payment.amount.total");
        currency ??= GatewayJson.Text(root, "request.payment.amount.currency");

        if (mapped == PaymentStatus.Confirmed && (approved is null || amount is null || currency is null))
            return null;

        var transactionId = approved is { } t
            ? GatewayJson.Text(t, "authorization") ?? GatewayJson.Text(t, "internalReference") ?? GatewayJson.Text(t, "receipt")
            : null;

        return new PaymentVerification(
            reference,
            mapped,
            amount ?? request.ExpectedAmount ?? 0m,
            currency ?? request.ExpectedCurrency ?? string.Empty,
            transactionId ?? GatewayJson.Text(root, "requestId"));
    }

    private static bool IsOk(JsonElement root) =>
        string.Equals(GatewayJson.Text(root, "status.status"), "OK", StringComparison.OrdinalIgnoreCase);

    private async Task<Result<(string Login, string SecretKey)>> CredentialsAsync(CancellationToken cancellationToken)
    {
        var login = await secretResolver.ResolveAsync(SecretTypes.GetnetLogin, null, cancellationToken);
        var secretKey = await secretResolver.ResolveAsync(SecretTypes.GetnetSecretKey, null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(secretKey))
            return Result<(string, string)>.Success((login, secretKey));

        logger.LogWarning("Getnet sin configurar: faltan los secretos {Login} o {SecretKey}", SecretTypes.GetnetLogin, SecretTypes.GetnetSecretKey);
        return Result<(string, string)>.Failure(DomainErrors.Integration.NotConfigured(System));
    }
}

/// <summary>Objeto <c>auth</c> y firmas de Getnet Web Checkout (API PlacetoPay).</summary>
public static class GetnetAuth
{
    /// <summary>
    /// <c>{ login, tranKey, nonce, seed }</c> con <c>tranKey = Base64(SHA-256(nonceCrudo + seed + secretKey))</c>,
    /// <c>nonce = Base64(nonceCrudo)</c> (16 bytes aleatorios) y <c>seed</c> = ahora en ISO-8601 con zona.
    /// </summary>
    public static Dictionary<string, string> Create(string login, string secretKey, DateTimeOffset now, byte[]? rawNonce = null)
    {
        var nonce = rawNonce ?? RandomNumberGenerator.GetBytes(16);
        var seed = IsoDate(now);
        return new Dictionary<string, string>
        {
            ["login"] = login,
            ["tranKey"] = TranKey(nonce, seed, secretKey),
            ["nonce"] = Convert.ToBase64String(nonce),
            ["seed"] = seed,
        };
    }

    public static string TranKey(byte[] rawNonce, string seed, string secretKey)
    {
        var seedBytes = Encoding.UTF8.GetBytes(seed);
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var data = new byte[rawNonce.Length + seedBytes.Length + keyBytes.Length];
        rawNonce.CopyTo(data, 0);
        seedBytes.CopyTo(data, rawNonce.Length);
        keyBytes.CopyTo(data, rawNonce.Length + seedBytes.Length);
        return Convert.ToBase64String(SHA256.HashData(data));
    }

    /// <summary>Fecha ISO-8601 con zona, sin fracciones: <c>2026-10-07T12:00:00-03:00</c>.</summary>
    public static string IsoDate(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    /// <summary>Firma de la notificación: acepta <c>sha256:&lt;hex&gt;</c> y el formato antiguo hex(SHA-1), en tiempo constante.</summary>
    public static bool VerifyNotificationSignature(string? signature, string requestId, string status, string date, string secretKey)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        var data = Encoding.UTF8.GetBytes(requestId + status + date + secretKey);
        const string Sha256Prefix = "sha256:";

        if (signature.StartsWith(Sha256Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var expected = Convert.ToHexStringLower(SHA256.HashData(data));
            return GatewayFormat.FixedTimeEquals(expected, signature[Sha256Prefix.Length..].Trim().ToLowerInvariant());
        }

#pragma warning disable CA5350 // SHA-1: formato antiguo que Getnet aún envía; solo se verifica, no se usa para firmar.
        var legacy = Convert.ToHexStringLower(SHA1.HashData(data));
#pragma warning restore CA5350
        return GatewayFormat.FixedTimeEquals(legacy, signature.Trim().ToLowerInvariant());
    }
}
