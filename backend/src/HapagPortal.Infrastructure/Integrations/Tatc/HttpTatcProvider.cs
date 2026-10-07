using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Tatc;

/// <summary>
/// Cliente Real del sistema de TATC (CT-TATC): <c>GET /bills-of-lading/{blNumber}/tatc</c> (404 = BL sin registro)
/// y la propuesta de la Ola F <c>POST /tatc/generation-requests</c> para la generación masiva, con la referencia
/// del portal como clave de idempotencia.
/// </summary>
public sealed class HttpTatcProvider(HttpClient httpClient, ISecretResolver secretResolver) : ITatcProvider
{
    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.Tatc, SecretTypes.TatcApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public async Task<Result<BlTatcRecord?>> GetByBlNumberAsync(string blNumber, CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<BlTatcDto>(
            httpClient, secretResolver, Endpoint, "getBlTatc", HttpMethod.Get,
            $"bills-of-lading/{IntegrationHttp.Segment(blNumber)}/tatc", cancellationToken);

        if (result.IsFailure)
            return Result<BlTatcRecord?>.Failure(result.Error);

        if (result.Value is not { } dto)
            return Result<BlTatcRecord?>.Success(null);

        return Result<BlTatcRecord?>.Success(new BlTatcRecord(
            dto.BlNumber,
            dto.Country,
            dto.UpdatedAt,
            dto.Containers
                .Select(c => new ContainerTatcRecord(c.ContainerNumber, c.TatcNumber, c.Status, c.IssuedAt, c.WarehouseCode, c.PendingReasons))
                .ToList()));
    }

    public async Task<Result<TatcGenerationReceipt>> RequestGenerationAsync(
        TatcGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = new GenerationRequestDto(
            request.RequestReference, request.Country, request.LocationCode, request.RequestedByTaxId, request.BlNumbers);

        var result = await IntegrationHttp.SendAsync<GenerationReceiptDto>(
            httpClient, secretResolver, Endpoint, "requestTatcGeneration", HttpMethod.Post,
            "tatc/generation-requests", cancellationToken, body, idempotencyKey: request.RequestReference);

        if (result.IsFailure)
            return Result<TatcGenerationReceipt>.Failure(result.Error);

        // 404 en una operación de alta: la ruta no existe en el sistema, no es un BL inexistente.
        if (result.Value is not { } dto)
            return Result<TatcGenerationReceipt>.Failure(DomainErrors.Integration.InvalidResponse(Endpoint.System));

        return Result<TatcGenerationReceipt>.Success(new TatcGenerationReceipt(
            dto.RequestId,
            dto.Items.Select(i => new TatcGenerationItem(i.BlNumber, i.Accepted, i.ReasonCode)).ToList()));
    }

    private sealed record ContainerTatcDto(
        string ContainerNumber,
        string Status,
        IReadOnlyList<string> PendingReasons,
        string? TatcNumber = null,
        DateTime? IssuedAt = null,
        string? WarehouseCode = null);

    private sealed record BlTatcDto(
        string BlNumber,
        string Country,
        IReadOnlyList<ContainerTatcDto> Containers,
        DateTime UpdatedAt);

    private sealed record GenerationRequestDto(
        string RequestReference,
        string Country,
        string LocationCode,
        string RequestedByTaxId,
        IReadOnlyList<string> BlNumbers);

    private sealed record GenerationItemDto(string BlNumber, bool Accepted, string? ReasonCode = null);

    private sealed record GenerationReceiptDto(string RequestId, IReadOnlyList<GenerationItemDto> Items);
}
