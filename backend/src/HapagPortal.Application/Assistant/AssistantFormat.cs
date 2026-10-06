namespace HapagPortal.Application.Assistant;

using System.Globalization;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;

/// <summary>
/// Presentación de los datos en las respuestas del asistente (en español; la traducción de las respuestas es
/// de Fase 2, M11). Las fechas se muestran en la hora del país con su huso (NF-22) y todo dato que el portal no
/// tiene se presenta como "no disponible", sin completarlo ni inferirlo (M10-03).
/// </summary>
public static class AssistantFormat
{
    public const string NotAvailable = "no disponible";

    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CL");

    private static readonly Dictionary<string, string> ShipmentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Booked"] = "Reservado",
        ["Loaded"] = "Cargado a bordo",
        ["InTransit"] = "En tránsito",
        ["Arrived"] = "Arribado",
        ["Discharged"] = "Descargado",
        ["Delivered"] = "Entregado",
        ["Active"] = "Activo",
        ["Cancelled"] = "Anulado",
    };

    private static readonly Dictionary<string, string> IssuanceStatuses = new(StringComparer.Ordinal)
    {
        [BlIssuanceStatuses.Pending] = "pendiente de emisión",
        [BlIssuanceStatuses.Issued] = "emitido",
        [BlIssuanceStatuses.AuthorizedAtDestination] = "emisión autorizada en destino",
        [BlIssuanceStatuses.IssuedAtDestination] = "emitido en destino",
        [BlIssuanceStatuses.Transferred] = "transferido",
        [BlIssuanceStatuses.Surrendered] = "entregado a la línea (surrender)",
        [BlIssuanceStatuses.TelexReleased] = "liberado por telex release",
        [BlIssuanceStatuses.Cancelled] = "anulado",
        [BlIssuanceStatuses.Unknown] = "estado no reconocido por el portal",
    };

    private static readonly Dictionary<string, string> TatcStatusNames = new(StringComparer.Ordinal)
    {
        [TatcStatuses.NotIssued] = "sin emitir",
        [TatcStatuses.PreTatc] = "pre-TATC generado",
        [TatcStatuses.Issued] = "emitido",
        [TatcStatuses.Cancelled] = "anulado",
        [TatcStatuses.Unknown] = "estado no reconocido por el portal",
        [TatcStatuses.PartiallyIssued] = "emitido para parte de los contenedores",
        [TatcStatuses.NotRegistered] = "sin registro en el sistema de TATC",
    };

    private static readonly Dictionary<string, string> DocumentTypeNames = new(StringComparer.Ordinal)
    {
        [ShipmentDocumentTypes.TransshipmentCertificate] = "Certificado de transbordo",
        [ShipmentDocumentTypes.GateOutCoupon] = "Cupón de retiro (Gate Out)",
        [ShipmentDocumentTypes.CollectReceipt] = "Comprobante Collect",
        [ShipmentDocumentTypes.BlCopyValued] = "Copia de BL valorada",
        [ShipmentDocumentTypes.BlCopyNonValued] = "Copia de BL no valorada",
        [ShipmentDocumentTypes.ResponsibilityLetter] = "Carta de responsabilidad",
        [ShipmentDocumentTypes.NoDebtCertificate] = "Certificado de libre deuda (CLD)",
    };

    public static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? NotAvailable : value.Trim();

    public static string LocalTime(DateTime? utc, string country) =>
        utc is null
            ? NotAvailable
            : $"{BusinessCalendar.ToLocal(country, utc.Value).ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)} ({BusinessCalendar.TimeZoneId(country)})";

    public static string Date(DateOnly? date) =>
        date?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) ?? NotAvailable;

    public static string Amount(decimal amount, string currency) =>
        $"{currency} {amount.ToString(currency == "CLP" ? "N0" : "N2", Spanish)}";

    public static string Operation(string operation) =>
        string.Equals(operation, "EXPORT", StringComparison.OrdinalIgnoreCase) ? "Exportación" : "Importación";

    public static string ShipmentStatus(string? status) =>
        status is null ? NotAvailable : ShipmentStatuses.GetValueOrDefault(status, status);

    public static string IssuanceStatus(string? status) =>
        status is null ? NotAvailable : IssuanceStatuses.GetValueOrDefault(status, status);

    public static string TatcStatus(string? status) =>
        status is null ? NotAvailable : TatcStatusNames.GetValueOrDefault(status, status);

    public static string DocumentType(string documentType) => DocumentTypeNames.GetValueOrDefault(documentType, documentType);
}
