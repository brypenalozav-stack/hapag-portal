namespace HapagPortal.Domain.Constants;

public static class DocumentPrefixes
{
    public const string Payment = "PAY-";
    public const string Receipt = "RCP-";
    public const string ServiceOrder = "ODS-";

    /// <summary>Solicitud de servicio on demand (M2-03, M2-04).</summary>
    public const string ServiceRequest = "SRV-";

    /// <summary>Boleta para el pago por depósito bancario (M5-02, M5-03).</summary>
    public const string DepositSlip = "BDP-";

    // Documentos del embarque (M6-01 a M6-07, Ola E).
    public const string TransshipmentCertificate = "CTB-";
    public const string GateOutCoupon = "CGO-";
    public const string CollectReceipt = "CCO-";
    public const string BlCopy = "CBL-";
    public const string ResponsibilityLetter = "CRE-";
    public const string NoDebtCertificate = "CLD-";

    public static string ForDocument(string documentType) => documentType switch
    {
        ShipmentDocumentTypes.TransshipmentCertificate => TransshipmentCertificate,
        ShipmentDocumentTypes.GateOutCoupon => GateOutCoupon,
        ShipmentDocumentTypes.CollectReceipt => CollectReceipt,
        ShipmentDocumentTypes.BlCopyValued or ShipmentDocumentTypes.BlCopyNonValued => BlCopy,
        ShipmentDocumentTypes.ResponsibilityLetter => ResponsibilityLetter,
        ShipmentDocumentTypes.NoDebtCertificate => NoDebtCertificate,
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Unknown document type.")
    };
}
