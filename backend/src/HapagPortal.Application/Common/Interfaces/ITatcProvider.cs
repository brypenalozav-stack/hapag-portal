using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Estado del TATC por BL y solicitud de generación masiva (CT-TATC, M2-09). El TATC se genera fuera del
/// portal; el portal consulta el estado vigente en el origen (solo con caché corta, nunca de una carga
/// anterior) y, para clientes con alto volumen en una localidad, envía en una sola solicitud la generación
/// de varios BL. BL sin registro en el sistema de TATC: <c>Success(null)</c>.
/// </summary>
public interface ITatcProvider
{
    Task<Result<BlTatcRecord?>> GetByBlNumberAsync(
        string blNumber,
        CancellationToken cancellationToken = default);

    Task<Result<TatcGenerationReceipt>> RequestGenerationAsync(
        TatcGenerationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Estado del TATC de cada contenedor del BL (<c>BlTatc</c> de CT-TATC).</summary>
public sealed record BlTatcRecord(
    string BlNumber,
    string Country,
    DateTime UpdatedAt,
    IReadOnlyList<ContainerTatcRecord> Containers);

/// <summary>Contenedor con su código de estado del origen (<c>TatcSourceStatuses</c>) y motivos pendientes.</summary>
public sealed record ContainerTatcRecord(
    string ContainerNumber,
    string? TatcNumber,
    string Status,
    DateTime? IssuedAt,
    string? WarehouseCode,
    IReadOnlyList<string> PendingReasons);

/// <summary>
/// Generación masiva para BL de una misma localidad (UN/LOCODE). <c>RequestReference</c> es el identificador
/// del portal (sirve de clave de idempotencia); <c>RequestedByTaxId</c> identifica a la organización solicitante.
/// </summary>
public sealed record TatcGenerationRequest(
    string RequestReference,
    string Country,
    string LocationCode,
    string RequestedByTaxId,
    IReadOnlyList<string> BlNumbers);

/// <summary>Respuesta del sistema de TATC: identificador de la solicitud y resultado por BL.</summary>
public sealed record TatcGenerationReceipt(string RequestId, IReadOnlyList<TatcGenerationItem> Items);

/// <summary>Resultado por BL: aceptado o rechazado con un motivo (<c>TatcBatchReasons</c> u otro del origen).</summary>
public sealed record TatcGenerationItem(string BlNumber, bool Accepted, string? ReasonCode);
