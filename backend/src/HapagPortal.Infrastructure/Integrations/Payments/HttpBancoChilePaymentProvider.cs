using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Cliente Real de Banco de Chile (CT-BCH). El inicio envía <c>Idempotency-Key</c> = referencia externa
/// (NF-01), así un reintento no crea otro pago. La verificación consulta <c>GET /payments/{reference}</c>
/// con la referencia del banco, que llega como <c>notificationToken</c>.
/// </summary>
public sealed class HttpBancoChilePaymentProvider(HttpClient httpClient, ISecretResolver secretResolver) : IPaymentProvider
{
    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.BancoChile, SecretTypes.BancoChileApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public string ProviderCode => IntegrationSystems.BancoChile;

    public bool VerifiesNotifications => true;

    public async Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = new InitiationRequestDto(
            request.ExternalReference,
            request.Amount,
            request.Currency,
            request.Subject,
            request.PayerTaxId,
            request.ReturnUrl,
            request.NotifyUrl);

        var result = await IntegrationHttp.SendAsync<InitiationDto>(
            httpClient, secretResolver, Endpoint, "initiatePayment", HttpMethod.Post, "payments", cancellationToken,
            body, idempotencyKey: request.ExternalReference);

        if (result.IsFailure)
            return Result<PaymentInitiation>.Failure(result.Error);

        return result.Value is { } dto
            ? Result<PaymentInitiation>.Success(new PaymentInitiation(
                dto.Reference, dto.ExternalReference, MapStatus(dto.Status), dto.RedirectUrl))
            : Result<PaymentInitiation>.Failure(DomainErrors.Integration.InvalidResponse(Endpoint.System));
    }

    /// <summary>
    /// Estado vigente del pago en el banco. La referencia devuelta es la que informa el banco: la
    /// comparación con <paramref name="externalReference"/> la hace quien verifica. Referencia
    /// desconocida (404) es <c>Integration.InvalidResponse</c>.
    /// </summary>
    public async Task<Result<PaymentVerification>> VerifyNotificationAsync(
        string notificationToken,
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<PaymentStatusDto>(
            httpClient, secretResolver, Endpoint, "getPayment", HttpMethod.Get,
            $"payments/{IntegrationHttp.Segment(notificationToken)}", cancellationToken);

        if (result.IsFailure)
            return Result<PaymentVerification>.Failure(result.Error);

        if (result.Value is not { } dto)
            return Result<PaymentVerification>.Failure(DomainErrors.Integration.InvalidResponse(Endpoint.System));

        return Result<PaymentVerification>.Success(new PaymentVerification(
            dto.ExternalReference,
            MapStatus(dto.Status),
            dto.Amount,
            dto.Currency,
            dto.TransactionId));
    }

    /// <summary>CONFIRMED es el único estado que confirma el cobro (CT-BCH).</summary>
    private static string MapStatus(string status) => status.ToUpperInvariant() switch
    {
        "CONFIRMED" => PaymentStatus.Confirmed,
        "REJECTED" or "EXPIRED" or "CANCELLED" => PaymentStatus.Failed,
        "AUTHORIZED" => PaymentStatus.Processing,
        _ => PaymentStatus.Pending,
    };

    private sealed record InitiationRequestDto(
        string ExternalReference,
        decimal Amount,
        string Currency,
        string Subject,
        string? PayerTaxId,
        string ReturnUrl,
        string NotifyUrl);

    private sealed record InitiationDto(string Reference, string ExternalReference, string Status, string RedirectUrl);

    private sealed record PaymentStatusDto(
        string Reference,
        string ExternalReference,
        string Status,
        decimal Amount,
        string Currency,
        string? TransactionId = null);
}
