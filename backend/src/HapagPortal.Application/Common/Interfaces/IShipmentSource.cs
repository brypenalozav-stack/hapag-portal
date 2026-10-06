using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Embarques desde FIS/Data Lake (CT-FIS). Mientras CT-FIS no esté validado, la carga sigue por
/// <c>POST bills-of-lading/import</c> (Q6). BL inexistente: <c>Success(null)</c>.
/// </summary>
public interface IShipmentSource
{
    Task<Result<ShipmentRecord?>> GetByBlNumberAsync(
        string blNumber,
        CancellationToken cancellationToken = default);

    /// <summary>Embarques modificados desde <paramref name="sinceUtc"/>; <paramref name="shipmentType"/> IMPORT, EXPORT o null (todos).</summary>
    Task<Result<IReadOnlyList<ShipmentRecord>>> GetUpdatedSinceAsync(
        DateTime sinceUtc,
        string? shipmentType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resumen de un embarque (<c>ShipmentSummary</c> de CT-FIS). Ola F agrega, como propuesta del contrato,
/// los datos de publicación por DIFU (M2-01: puerto de descarga, destino final y DIFU con su localidad) y de
/// emisión del documento de transporte (M2-02: tipo BL/SWB/EBL, plataforma del EBL, código de estado de
/// emisión del origen, su fecha y el lugar de emisión). Son opcionales: nulo = el origen no lo informa.
/// </summary>
public sealed record ShipmentRecord(
    string BlNumber,
    string? BookingNumber,
    string ShipmentType,
    string? Vessel,
    string? Voyage,
    DateTime? Etd,
    DateTime? Eta,
    int? FreeDays,
    string? DepotImport,
    string? DepotExport,
    string? MatchCode,
    string? TaxId,
    string? PortOfDischargeCode = null,
    string? FinalDestinationCode = null,
    string? DifuCode = null,
    string? DifuLocationCode = null,
    string? DocumentType = null,
    string? EblPlatform = null,
    string? IssuanceStatus = null,
    DateTime? IssuanceStatusAt = null,
    string? IssuancePlace = null);
