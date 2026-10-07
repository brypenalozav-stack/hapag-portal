using System.Net.Http.Headers;
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

/// <summary>Opciones de Bci Pagos (<c>Integrations:BciPagos</c>).</summary>
public sealed class BciPagosOptions
{
    public string ShopCountry { get; set; } = "CL";

    /// <summary>Correo que se envía si el usuario no tiene correo en la sesión (x_customer_email es obligatorio).</summary>
    public string? FallbackCustomerEmail { get; set; }

    /// <summary>Vigencia del JWT de consulta si el token no trae <c>exp</c>.</summary>
    public int TokenCacheMinutes { get; set; } = 10;

    /// <summary>Textos (sin distinguir mayúsculas, por contención) del estado que significan pagado.</summary>
    public string[] PaidStatusValues { get; set; } = ["completed", "complete", "paid", "aprobad", "exitos"];

    /// <summary>Textos del estado que significan rechazado o anulado.</summary>
    public string[] FailedStatusValues { get; set; } = ["rechaz", "reject", "failed", "fallid", "anulad", "cancel", "expir"];
}

/// <summary>Caché del JWT de consulta de Bci Pagos (singleton): se pide de nuevo al vencer o ante un 401.</summary>
public sealed class BciPagosTokenCache
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string?> GetAsync(DateTimeOffset now, Func<Task<(string? Token, DateTimeOffset ExpiresAt)>> login)
    {
        if (_token is not null && now < _expiresAt)
            return _token;

        await _lock.WaitAsync();
        try
        {
            if (_token is not null && now < _expiresAt)
                return _token;

            var (token, expiresAt) = await login();
            _token = token;
            _expiresAt = expiresAt;
            return token;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _token = null;
}

/// <summary>
/// Botón BCI = Bci Pagos (ex Pago Fácil), API REST https://apis.pgf.cl (desarrollo https://apis-dev.pgf.cl):
/// <list type="bullet">
/// <item>Crear: <c>POST /trxs</c> sin cabecera de autorización; la autenticidad la da <c>x_signature</c>
/// (<see cref="BciPagosSignature"/>) con <c>BCIPAGOS_TOKEN_SECRET</c>; <c>x_account_id</c> = <c>BCIPAGOS_ACCOUNT_ID</c>.
/// El pagador va a la opción "gateway" de <c>pay_url</c> (lectura tolerante: arreglo de textos u objetos).</item>
/// <item>Consultar: <c>POST /users/login</c> (<c>BCIPAGOS_USERNAME</c>/<c>BCIPAGOS_PASSWORD</c>, JWT en caché) y
/// <c>GET /trxs/{id_trx}</c>; los textos de estado pagado/rechazado son configurables.</item>
/// <item>Notificación en <c>x_url_callback</c>: formato no publicado; si trae <c>x_signature</c> se verifica y, en
/// cualquier caso, el estado se consulta siempre. No hay anulación por API: el cobro vence solo.</item>
/// </list>
/// </summary>
public sealed class BciPagosPaymentProvider(
    HttpClient httpClient,
    ISecretResolver secretResolver,
    BciPagosOptions options,
    BciPagosTokenCache tokenCache,
    PaymentPublicUrls publicUrls,
    TimeProvider timeProvider,
    ILogger<BciPagosPaymentProvider> logger) : IPaymentProvider
{
    private const string System = IntegrationSystems.BciPagos;

    public string ProviderCode => PaymentProviderKeys.Bci;

    public bool VerifiesNotifications => true;

    public async Task<Result<PaymentInitiation>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
    {
        var accountId = await secretResolver.ResolveAsync(SecretTypes.BciPagosAccountId, null, cancellationToken);
        var tokenSecret = await secretResolver.ResolveAsync(SecretTypes.BciPagosTokenSecret, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(tokenSecret))
            return NotConfigured<PaymentInitiation>($"{SecretTypes.BciPagosAccountId}/{SecretTypes.BciPagosTokenSecret}");

        var email = string.IsNullOrWhiteSpace(request.PayerEmail) ? options.FallbackCustomerEmail : request.PayerEmail;
        if (string.IsNullOrWhiteSpace(email))
            return NotConfigured<PaymentInitiation>($"Integrations:{System}:FallbackCustomerEmail");

        var returnUrl = publicUrls.Site(request.ReturnUrl);
        var notifyUrl = publicUrls.Api(request.NotifyUrl);
        if (returnUrl.IsFailure || notifyUrl.IsFailure)
            return Result<PaymentInitiation>.Failure(returnUrl.IsFailure ? returnUrl.Error : notifyUrl.Error);

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["x_account_id"] = accountId,
            ["x_amount"] = GatewayFormat.AmountText(request.Amount, request.Currency),
            ["x_currency"] = request.Currency.ToUpperInvariant(),
            ["x_reference"] = request.ExternalReference,
            ["x_customer_email"] = email,
            ["x_url_complete"] = returnUrl.Value,
            ["x_url_callback"] = notifyUrl.Value,
            ["x_url_cancel"] = returnUrl.Value,
            ["x_shop_country"] = options.ShopCountry,
            ["x_session_id"] = GatewayFormat.Truncate(request.ExternalReference, 61),
        };

        var body = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in fields)
            body[key] = value;
        body["x_amount"] = GatewayFormat.Amount(request.Amount, request.Currency);
        body["x_signature"] = BciPagosSignature.Sign(fields, tokenSecret);

        var response = await GatewayHttp.SendAsync(httpClient, System, "createTrx", HttpMethod.Post, "trxs", cancellationToken, body);
        if (response.IsFailure)
            return Result<PaymentInitiation>.Failure(response.Error);

        var idTrx = GatewayJson.Text(response.Value, "body.data.id_trx", "data.id_trx", "id_trx");
        var payUrl = PayUrl(response.Value);
        if (idTrx is null || payUrl is null)
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.InvalidResponse(System));

        return Result<PaymentInitiation>.Success(new PaymentInitiation(idTrx, request.ExternalReference, PaymentStatus.Pending, payUrl));
    }

    public async Task<Result<PaymentVerification>> GetStatusAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
            return Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(System));

        var path = $"trxs/{Uri.EscapeDataString(request.ProviderReference)}";
        Result<JsonElement> response = Result<JsonElement>.Failure(DomainErrors.Integration.NotConfigured(System));

        // Un 401 con un token en caché puede ser un token vencido: se pide otro una vez.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var token = await TokenAsync(cancellationToken);
            if (token.IsFailure)
                return Result<PaymentVerification>.Failure(token.Error);

            response = await GatewayHttp.SendAsync(
                httpClient, System, "getTrx", HttpMethod.Get, path, cancellationToken,
                authorization: new AuthenticationHeaderValue("Bearer", token.Value));
            if (response.IsSuccess || response.Error.Code != "Integration.InvalidResponse")
                break;

            tokenCache.Invalidate();
        }

        if (response.IsFailure)
            return Result<PaymentVerification>.Failure(response.Error);

        var data = GatewayJson.Find(response.Value, "data") ?? GatewayJson.Find(response.Value, "body.data") ?? response.Value;
        var amount = GatewayJson.Decimal(data, "amount", "x_amount", "monto");
        if (amount is null)
            return Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(System));

        return Result<PaymentVerification>.Success(new PaymentVerification(
            GatewayJson.Text(data, "order_id_tienda", "x_reference", "reference") ?? string.Empty,
            MapStatus(data, options),
            amount.Value,
            GatewayJson.Text(data, "currency", "x_currency") ?? "CLP",
            GatewayJson.Text(data, "auth_code", "id_trx") ?? request.ProviderReference));
    }

    /// <summary>Bci Pagos no publica anulación: el cobro pendiente vence solo.</summary>
    public Task<Result> CancelAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    public async Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        var fields = ParseFields(notification.RawBody, notification.ContentType);
        if (fields is null)
            return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);

        if (fields.TryGetValue("x_signature", out var signature))
        {
            var tokenSecret = await secretResolver.ResolveAsync(SecretTypes.BciPagosTokenSecret, null, cancellationToken);
            if (string.IsNullOrWhiteSpace(tokenSecret) || !BciPagosSignature.Verify(fields, signature, tokenSecret))
            {
                logger.LogWarning("Notificación de Bci Pagos con firma inválida o sin {SecretType}", SecretTypes.BciPagosTokenSecret);
                return Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);
            }
        }

        var idTrx = Value(fields, "x_id_trx", "id_trx", "x_transaction_id");
        var reference = Value(fields, "x_reference", "order_id_tienda");
        return idTrx is null && reference is null
            ? Result<PaymentNotificationInfo>.Failure(Error.Unauthorized)
            : Result<PaymentNotificationInfo>.Success(new PaymentNotificationInfo(idTrx, reference));
    }

    /// <summary>
    /// Pagado: <c>responce_code</c> "0" con <c>auth_code</c>, o un estado que contiene un texto de pagado; fallido: un
    /// estado que contiene un texto de rechazo; el resto, pendiente. El estado puede ser texto u objeto (name/code).
    /// </summary>
    public static string MapStatus(JsonElement data, BciPagosOptions options)
    {
        var status = GatewayJson.Text(data, "status.name", "status.code", "status.description", "status", "estado") ?? string.Empty;

        if (options.FailedStatusValues.Any(v => status.Contains(v, StringComparison.OrdinalIgnoreCase)))
            return PaymentStatus.Failed;

        var responseCode = GatewayJson.Text(data, "responce_code", "response_code");
        var authCode = GatewayJson.Text(data, "auth_code");
        if ((responseCode == "0" && !string.IsNullOrWhiteSpace(authCode)) ||
            options.PaidStatusValues.Any(v => status.Contains(v, StringComparison.OrdinalIgnoreCase)))
        {
            return PaymentStatus.Confirmed;
        }

        return PaymentStatus.Pending;
    }

    /// <summary>URL de pago: la opción "gateway" de <c>pay_url</c> (textos u objetos url/link/href) o, si no, la primera.</summary>
    public static string? PayUrl(JsonElement root)
    {
        var payUrl = GatewayJson.Find(root, "body.data.pay_url") ?? GatewayJson.Find(root, "data.pay_url") ?? GatewayJson.Find(root, "pay_url");
        if (payUrl is not { } value)
            return null;

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString();

        var candidates = new List<(string Url, string Text)>();
        IEnumerable<JsonElement> items = value.ValueKind switch
        {
            JsonValueKind.Array => value.EnumerateArray(),
            JsonValueKind.Object => value.EnumerateObject().Select(p => p.Value),
            _ => []
        };

        foreach (var item in items)
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } text)
                candidates.Add((text, text));
            else if (item.ValueKind == JsonValueKind.Object && GatewayJson.Text(item, "url", "link", "href") is { } url)
                candidates.Add((url, item.GetRawText()));
        }

        return candidates.FirstOrDefault(c => c.Text.Contains("gateway", StringComparison.OrdinalIgnoreCase)).Url
            ?? candidates.FirstOrDefault().Url;
    }

    private async Task<Result<string>> TokenAsync(CancellationToken cancellationToken)
    {
        var username = await secretResolver.ResolveAsync(SecretTypes.BciPagosUsername, null, cancellationToken);
        var password = await secretResolver.ResolveAsync(SecretTypes.BciPagosPassword, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return NotConfigured<string>($"{SecretTypes.BciPagosUsername}/{SecretTypes.BciPagosPassword}");

        Error? loginError = null;
        var token = await tokenCache.GetAsync(timeProvider.GetUtcNow(), async () =>
        {
            var response = await GatewayHttp.SendAsync(
                httpClient, System, "login", HttpMethod.Post, "users/login", cancellationToken,
                new Dictionary<string, string> { ["username"] = username, ["password"] = password });
            if (response.IsFailure)
            {
                loginError = response.Error;
                return (null, default);
            }

            var jwt = GatewayJson.Text(response.Value, "token", "access_token", "body.data.token", "data.token", "body.token");
            return (jwt, ExpiresAt(jwt));
        });

        return token is null
            ? Result<string>.Failure(loginError ?? DomainErrors.Integration.InvalidResponse(System))
            : Result<string>.Success(token);
    }

    /// <summary>Vencimiento del JWT (<c>exp</c>) con un minuto de margen; sin <c>exp</c>, la vigencia configurada.</summary>
    private DateTimeOffset ExpiresAt(string? jwt)
    {
        var now = timeProvider.GetUtcNow();
        var fallback = now.AddMinutes(Math.Max(1, options.TokenCacheMinutes));
        var parts = jwt?.Split('.');
        if (parts is not { Length: 3 })
            return fallback;

        try
        {
            using var payload = JsonDocument.Parse(Base64UrlTextEncoder.Decode(parts[1]));
            return GatewayJson.Decimal(payload.RootElement, "exp") is { } exp
                ? DateTimeOffset.FromUnixTimeSeconds((long)exp).AddMinutes(-1)
                : fallback;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            return fallback;
        }
    }

    /// <summary>Campos escalares de primer nivel del cuerpo (JSON o formulario).</summary>
    private static Dictionary<string, string>? ParseFields(string rawBody, string? contentType)
    {
        var body = rawBody.Trim();
        if (body.StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var fields = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number => property.Value.GetRawText(),
                        JsonValueKind.True => "1",
                        JsonValueKind.False => "",
                        _ => null
                    };
                    if (value is not null)
                        fields[property.Name] = value;
                }

                return fields;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (body.Length == 0 || (contentType is not null && !contentType.Contains("form", StringComparison.OrdinalIgnoreCase)))
            return null;

        return QueryHelpers.ParseQuery(body).ToDictionary(kv => kv.Key, kv => kv.Value.ToString(), StringComparer.Ordinal);
    }

    private static string? Value(Dictionary<string, string> fields, params string[] names) =>
        names.Select(n => fields.GetValueOrDefault(n)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private Result<T> NotConfigured<T>(string what)
    {
        logger.LogWarning("Bci Pagos sin configurar: falta {Missing}", what);
        return Result<T>.Failure(DomainErrors.Integration.NotConfigured(System));
    }
}

/// <summary>
/// Firma de Bci Pagos (SignatureHelper del SDK oficial): claves escalares de primer nivel salvo <c>x_signature</c>,
/// en orden ordinal, concatenadas como clave + valor sin separadores; <c>hex minúscula(HMAC-SHA256(mensaje, token_secret))</c>.
/// </summary>
public static class BciPagosSignature
{
    public const string SignatureField = "x_signature";

    public static string Message(IEnumerable<KeyValuePair<string, string>> fields)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in fields.Where(f => f.Key != SignatureField).OrderBy(f => f.Key, StringComparer.Ordinal))
            builder.Append(key).Append(value);
        return builder.ToString();
    }

    public static string Sign(IEnumerable<KeyValuePair<string, string>> fields, string tokenSecret) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(tokenSecret), Encoding.UTF8.GetBytes(Message(fields))));

    public static bool Verify(IReadOnlyDictionary<string, string> fields, string? signature, string tokenSecret) =>
        !string.IsNullOrWhiteSpace(signature) &&
        GatewayFormat.FixedTimeEquals(Sign(fields, tokenSecret), signature.Trim().ToLowerInvariant());
}
