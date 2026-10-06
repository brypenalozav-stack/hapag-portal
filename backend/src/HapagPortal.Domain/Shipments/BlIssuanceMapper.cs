using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Shipments;

/// <summary>
/// Traducción de los códigos de emisión del origen (CT-FIS) a los del portal (M2-02). Lo que el origen no
/// informa queda nulo y un código desconocido queda en <see cref="BlIssuanceStatuses.Unknown"/>: el portal
/// muestra el estado registrado en el origen y nunca lo infiere.
/// </summary>
public static class BlIssuanceMapper
{
    private static readonly Dictionary<string, string> Statuses = new(StringComparer.OrdinalIgnoreCase)
    {
        [BlIssuanceSourceStatuses.NotIssued] = BlIssuanceStatuses.Pending,
        [BlIssuanceSourceStatuses.Draft] = BlIssuanceStatuses.Pending,
        [BlIssuanceSourceStatuses.Issued] = BlIssuanceStatuses.Issued,
        [BlIssuanceSourceStatuses.DestinationIssuanceAuthorized] = BlIssuanceStatuses.AuthorizedAtDestination,
        [BlIssuanceSourceStatuses.IssuedAtDestination] = BlIssuanceStatuses.IssuedAtDestination,
        [BlIssuanceSourceStatuses.Transferred] = BlIssuanceStatuses.Transferred,
        [BlIssuanceSourceStatuses.Surrendered] = BlIssuanceStatuses.Surrendered,
        [BlIssuanceSourceStatuses.TelexReleased] = BlIssuanceStatuses.TelexReleased,
        [BlIssuanceSourceStatuses.Voided] = BlIssuanceStatuses.Cancelled,
    };

    private static readonly Dictionary<string, string> DocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BL"] = TransportDocumentTypes.Bl,
        ["OBL"] = TransportDocumentTypes.Bl,
        ["ORIGINAL_BL"] = TransportDocumentTypes.Bl,
        ["SWB"] = TransportDocumentTypes.Swb,
        ["SEA_WAYBILL"] = TransportDocumentTypes.Swb,
        ["EBL"] = TransportDocumentTypes.Ebl,
        ["ELECTRONIC_BL"] = TransportDocumentTypes.Ebl,
    };

    /// <summary>Estado del portal para el código del origen; nulo si el origen no lo informó.</summary>
    public static string? MapStatus(string? sourceStatus)
    {
        if (string.IsNullOrWhiteSpace(sourceStatus))
            return null;

        return Statuses.TryGetValue(sourceStatus.Trim(), out var status) ? status : BlIssuanceStatuses.Unknown;
    }

    /// <summary>Tipo de documento del portal; nulo si el origen no lo informó o no se reconoce.</summary>
    public static string? MapDocumentType(string? sourceType)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            return null;

        return DocumentTypes.TryGetValue(sourceType.Trim(), out var type) ? type : null;
    }
}
