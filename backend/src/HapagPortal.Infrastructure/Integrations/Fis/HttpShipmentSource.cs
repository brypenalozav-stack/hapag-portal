using System.Globalization;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Fis;

/// <summary>
/// Cliente Real de FIS/Data Lake (CT-FIS). BL inexistente (404): <c>Success(null)</c>. La consulta de
/// cambios recorre las páginas siguiendo <c>nextCursor</c>, con un tope de páginas para no quedar en un
/// ciclo si el sistema devuelve siempre un cursor.
/// </summary>
public sealed class HttpShipmentSource(HttpClient httpClient, ISecretResolver secretResolver) : IShipmentSource
{
    private const int PageSize = 100;
    private const int MaxPages = 50;

    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.Fis, SecretTypes.FisApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public async Task<Result<ShipmentRecord?>> GetByBlNumberAsync(
        string blNumber,
        CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<ShipmentDto>(
            httpClient, secretResolver, Endpoint, "getShipmentByBlNumber", HttpMethod.Get,
            $"shipments/{IntegrationHttp.Segment(blNumber)}", cancellationToken);

        if (result.IsFailure)
            return Result<ShipmentRecord?>.Failure(result.Error);

        return Result<ShipmentRecord?>.Success(result.Value is { } dto ? ToRecord(dto) : null);
    }

    public async Task<Result<IReadOnlyList<ShipmentRecord>>> GetUpdatedSinceAsync(
        DateTime sinceUtc,
        string? shipmentType,
        CancellationToken cancellationToken = default)
    {
        var records = new List<ShipmentRecord>();
        var since = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        string? cursor = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var uri = IntegrationHttp.WithQuery(
                "shipments",
                ("updatedSince", since),
                ("type", shipmentType),
                ("cursor", cursor),
                ("limit", PageSize.ToString(CultureInfo.InvariantCulture)));

            var result = await IntegrationHttp.SendAsync<ShipmentPageDto>(
                httpClient, secretResolver, Endpoint, "getShipmentsUpdatedSince", HttpMethod.Get, uri, cancellationToken);

            if (result.IsFailure)
                return Result<IReadOnlyList<ShipmentRecord>>.Failure(result.Error);

            // 404: sin datos, como en el resto de los puertos de lista.
            if (result.Value is null)
                return Result<IReadOnlyList<ShipmentRecord>>.Success(records);

            records.AddRange(result.Value.Items.Select(ToRecord));
            cursor = result.Value.NextCursor;

            if (string.IsNullOrEmpty(cursor))
                return Result<IReadOnlyList<ShipmentRecord>>.Success(records);
        }

        return Result<IReadOnlyList<ShipmentRecord>>.Failure(DomainErrors.Integration.InvalidResponse(Endpoint.System));
    }

    private static ShipmentRecord ToRecord(ShipmentDto dto) => new(
        dto.BlNumber,
        dto.BookingNumber,
        dto.ShipmentType,
        dto.Vessel,
        dto.Voyage,
        dto.Etd,
        dto.Eta,
        dto.FreeDays,
        dto.DepotImport,
        dto.DepotExport,
        dto.MatchCode,
        dto.TaxId);

    private sealed record ShipmentPageDto(IReadOnlyList<ShipmentDto> Items, string? NextCursor);

    /// <summary><c>ShipmentSummary</c> de CT-FIS; el detalle por BL trae más campos, que se ignoran.</summary>
    private sealed record ShipmentDto(
        string BlNumber,
        string ShipmentType,
        DateTime UpdatedAt,
        string? BookingNumber = null,
        string? Vessel = null,
        string? Voyage = null,
        DateTime? Etd = null,
        DateTime? Eta = null,
        int? FreeDays = null,
        string? DepotImport = null,
        string? DepotExport = null,
        string? MatchCode = null,
        string? TaxId = null);
}
