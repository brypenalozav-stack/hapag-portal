using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Cliente Real de Khipu (CT-KHIPU, API pública v3, JSON snake_case, cabecera <c>x-api-key</c> con el
/// secreto <c>KHIPU_SECRET</c>). Verifica las notificaciones (<see cref="VerifiesNotifications"/> = true):
/// el webhook toma el estado, la referencia y el monto de esta consulta, no del cuerpo recibido.
/// </summary>
public sealed class HttpKhipuPaymentProvider(HttpClient httpClient, ISecretResolver secretResolver) : IPaymentProvider
{
    private const string KhipuStatusDone = "done";

    // status_detail que anulan el pago aunque status no sea "pending" (CT-KHIPU, PaymentResponse).
    private static readonly HashSet<string> FailedStatusDetails =
        new(["rejected-by-payer", "marked-as-abuse", "reversed"], StringComparer.OrdinalIgnoreCase);

    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.Khipu, SecretTypes.KhipuSecret, "x-api-key", IntegrationHttp.SnakeCaseJson);

    public string ProviderCode => IntegrationSystems.Khipu;

    public bool VerifiesNotifications => true;

    public async Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = new CreatePaymentDto(
            request.Amount,
            request.Currency,
            request.Subject,
            request.ExternalReference,
            request.ReturnUrl,
            request.NotifyUrl,
            NotifyApiVersion: "3.0",
            FixedPayerPersonalIdentifier: request.PayerTaxId);

        var result = await IntegrationHttp.SendAsync<CreatePaymentResponseDto>(
            httpClient, secretResolver, Endpoint, "createPayment", HttpMethod.Post, "v3/payments", cancellationToken, body);

        if (result.IsFailure)
            return Result<PaymentInitiation>.Failure(result.Error);

        return result.Value is { } dto
            ? Result<PaymentInitiation>.Success(new PaymentInitiation(
                dto.PaymentId, request.ExternalReference, PaymentStatus.Pending, dto.PaymentUrl))
            : Result<PaymentInitiation>.Failure(DomainErrors.Integration.InvalidResponse(Endpoint.System));
    }

    /// <summary>
    /// Consulta el pago por <c>notification_token</c> (<c>GET /v3/payments?notification_token=</c>).
    /// NOTA: CT-KHIPU (docs/integraciones/contratos/khipu.openapi.yaml, operación
    /// getPaymentByNotificationToken) deja pendiente que Finanzas – Fer confirme que la v3 mantiene esta
    /// consulta; si no, la verificación pasa a <c>GET /v3/payments/{id}</c> con el <c>payment_id</c> de la
    /// notificación. Se implementa el contrato tal como está escrito.
    /// <para>
    /// La referencia devuelta es el <c>transaction_id</c> que informa Khipu: la comparación con
    /// <paramref name="externalReference"/> la hace el webhook. Token desconocido (404) es
    /// <c>Integration.InvalidResponse</c>.
    /// </para>
    /// </summary>
    public async Task<Result<PaymentVerification>> VerifyNotificationAsync(
        string notificationToken,
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        var uri = IntegrationHttp.WithQuery("v3/payments", ("notification_token", notificationToken));
        var result = await IntegrationHttp.SendAsync<PaymentDto>(
            httpClient, secretResolver, Endpoint, "getPaymentByNotificationToken", HttpMethod.Get, uri, cancellationToken);

        if (result.IsFailure)
            return Result<PaymentVerification>.Failure(result.Error);

        if (result.Value is not { } dto)
            return Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(Endpoint.System));

        return Result<PaymentVerification>.Success(new PaymentVerification(
            dto.TransactionId,
            MapStatus(dto.Status, dto.StatusDetail),
            dto.Amount,
            dto.Currency,
            dto.PaymentId));
    }

    private static string MapStatus(string status, string? statusDetail)
    {
        if (statusDetail is not null && FailedStatusDetails.Contains(statusDetail))
            return PaymentStatus.Failed;

        return string.Equals(status, KhipuStatusDone, StringComparison.OrdinalIgnoreCase)
            ? PaymentStatus.Confirmed
            : PaymentStatus.Processing;
    }

    private sealed record CreatePaymentDto(
        decimal Amount,
        string Currency,
        string Subject,
        string TransactionId,
        string ReturnUrl,
        string NotifyUrl,
        string NotifyApiVersion,
        string? FixedPayerPersonalIdentifier);

    private sealed record CreatePaymentResponseDto(string PaymentId, string PaymentUrl);

    private sealed record PaymentDto(
        string PaymentId,
        string NotificationToken,
        long ReceiverId,
        string Subject,
        decimal Amount,
        string Currency,
        string Status,
        string TransactionId,
        string? StatusDetail = null);
}
