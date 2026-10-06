using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Cliente Real del Counter de Nexus (CT-COUNTER, propuesta Ola I): <c>GET</c> y <c>PUT counter/bills-of-lading/{blNumber}</c>
/// con la clave de API de Nexus. El <c>PUT</c> lleva <c>Idempotency-Key</c>; un BL sin registro (404) se lee como nulo.
/// </summary>
public sealed class HttpCounterRecorder(HttpClient httpClient, ISecretResolver secretResolver) : ICounterRecorder
{
    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.Nexus, SecretTypes.NexusApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public async Task<Result<CounterSourceRecord>> RecordAsync(
        CounterRecordRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var body = new CounterRecordBody(
            request.Country, request.ExchangeDate, request.HblReceived, request.HblReceivedAt, request.Deconsolidated,
            request.DeconsolidatedAt, request.Notes, request.RecordedBy);

        var result = await IntegrationHttp.SendAsync<CounterRecordPayload>(
            httpClient, secretResolver, Endpoint, "putCounterRecord", HttpMethod.Put,
            $"counter/bills-of-lading/{IntegrationHttp.Segment(request.BlNumber)}", cancellationToken, body, idempotencyKey);

        if (result.IsFailure)
            return Result<CounterSourceRecord>.Failure(result.Error);

        // Un PUT exitoso siempre devuelve el registro; un 404 aquí incumple el contrato.
        return result.Value is null
            ? Result<CounterSourceRecord>.Failure(DomainErrors.Integration.InvalidResponse(IntegrationSystems.Nexus))
            : Result<CounterSourceRecord>.Success(result.Value.ToRecord());
    }

    public async Task<Result<CounterSourceRecord?>> GetAsync(string blNumber, CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<CounterRecordPayload>(
            httpClient, secretResolver, Endpoint, "getCounterRecord", HttpMethod.Get,
            $"counter/bills-of-lading/{IntegrationHttp.Segment(blNumber)}", cancellationToken);

        return result.IsFailure
            ? Result<CounterSourceRecord?>.Failure(result.Error)
            : Result<CounterSourceRecord?>.Success(result.Value?.ToRecord());
    }

    private sealed record CounterRecordBody(
        string Country,
        DateOnly? ExchangeDate,
        bool HblReceived,
        DateOnly? HblReceivedAt,
        bool Deconsolidated,
        DateOnly? DeconsolidatedAt,
        string? Notes,
        string RecordedBy);

    private sealed record CounterRecordPayload(
        string BlNumber,
        string Country,
        bool HblReceived,
        bool Deconsolidated,
        string SourceReference,
        DateTime RecordedAt,
        DateOnly? ExchangeDate = null,
        DateOnly? HblReceivedAt = null,
        DateOnly? DeconsolidatedAt = null,
        string? RecordedBy = null)
    {
        public CounterSourceRecord ToRecord() => new(
            BlNumber, Country, ExchangeDate, HblReceived, HblReceivedAt, Deconsolidated, DeconsolidatedAt, SourceReference, RecordedAt, RecordedBy);
    }
}
