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

    /// <summary>Imputación de cargos a la línea de crédito (M5-10, Ola H).</summary>
    public const string CreditImputation = "CRI-";

    // Documentos del embarque (M6-01 a M6-07, Ola E).
    public const string TransshipmentCertificate = "CTB-";
    public const string GateOutCoupon = "CGO-";
    public const string CollectReceipt = "CCO-";
    public const string BlCopy = "CBL-";
    public const string ResponsibilityLetter = "CRE-";
    public const string NoDebtCertificate = "CLD-";

    /// <summary>Recibo del pago anticipado de Gate Out antes de la factura (M3-19, Ola H).</summary>
    public const string GateOutAdvanceReceipt = "RGO-";

    /// <summary>Certificado de flete (M6-02) y carta de liberación y desconsolidado (M6-08), Bolivia (Ola J).</summary>
    public const string FreightCertificate = "CFL-";
    public const string ReleaseLetter = "CLB-";

    public static string ForDocument(string documentType) => documentType switch
    {
        ShipmentDocumentTypes.TransshipmentCertificate => TransshipmentCertificate,
        ShipmentDocumentTypes.GateOutCoupon => GateOutCoupon,
        ShipmentDocumentTypes.CollectReceipt => CollectReceipt,
        ShipmentDocumentTypes.BlCopyValued or ShipmentDocumentTypes.BlCopyNonValued => BlCopy,
        ShipmentDocumentTypes.ResponsibilityLetter => ResponsibilityLetter,
        ShipmentDocumentTypes.NoDebtCertificate => NoDebtCertificate,
        ShipmentDocumentTypes.GateOutAdvanceReceipt => GateOutAdvanceReceipt,
        ShipmentDocumentTypes.FreightCertificate => FreightCertificate,
        ShipmentDocumentTypes.ReleaseLetter => ReleaseLetter,
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Unknown document type.")
    };
}
