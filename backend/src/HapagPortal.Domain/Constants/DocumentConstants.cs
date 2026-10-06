namespace HapagPortal.Domain.Constants;

/// <summary>
/// Tipos de documento del repositorio documental del embarque (M6-09). Cada tipo se rige por una acción
/// de la matriz de M1-11 (ver <see cref="ShipmentDocumentAccess"/>).
/// </summary>
public static class ShipmentDocumentTypes
{
    /// <summary>Certificado de transbordo (M6-01, CL-EXP-08, CL-IMP-07). Firmado.</summary>
    public const string TransshipmentCertificate = "TransshipmentCertificate";

    /// <summary>Cupón de retiro de Gate Out (M6-03, CL-EXP-02).</summary>
    public const string GateOutCoupon = "GateOutCoupon";

    /// <summary>Comprobante Collect, solo para las agencias de aduanas autorizadas (M6-04).</summary>
    public const string CollectReceipt = "CollectReceipt";

    /// <summary>Copia del BL valorada, con los valores comerciales (M6-05).</summary>
    public const string BlCopyValued = "BlCopyValued";

    /// <summary>Copia del BL no valorada, sin valores comerciales (M6-05).</summary>
    public const string BlCopyNonValued = "BlCopyNonValued";

    /// <summary>Carta de responsabilidad (M6-06, CL-IMP-11, BO-IMP-10); obligatoria para FFWW (M4-04).</summary>
    public const string ResponsibilityLetter = "ResponsibilityLetter";

    /// <summary>Certificado de libre deuda, importación de Bolivia (M6-07, BO-IMP-15). Firmado.</summary>
    public const string NoDebtCertificate = "NoDebtCertificate";

    /// <summary>
    /// Recibo del pago anticipado de Gate Out antes de la emisión de la factura (M3-19, Ola H): identifica el
    /// embarque, las unidades y el pagador; la factura emitida tras el zarpe se vincula a él.
    /// </summary>
    public const string GateOutAdvanceReceipt = "GateOutAdvanceReceipt";

    public static readonly string[] All =
    [
        TransshipmentCertificate, GateOutCoupon, CollectReceipt, BlCopyValued, BlCopyNonValued,
        ResponsibilityLetter, NoDebtCertificate, GateOutAdvanceReceipt
    ];

    /// <summary>Documentos que se emiten con firma electrónica (M6-01, M6-02, M6-07) por <c>IDocumentSigner</c>.</summary>
    public static readonly string[] Signed = [TransshipmentCertificate, NoDebtCertificate];
}

/// <summary>
/// Acción de M1-11 que habilita ver y descargar cada tipo de documento (M6-09, NF-05). El comprobante
/// Collect queda limitado a la columna de agencias de aduanas (M6-04); la copia valorada, a quien puede
/// solicitarla (el shipper solo accede a la no valorada, M6-05).
/// </summary>
public static class ShipmentDocumentAccess
{
    public static string ActionFor(string documentType) => documentType switch
    {
        ShipmentDocumentTypes.TransshipmentCertificate => ShipmentActionCodes.GenerateTransshipmentCertificate,
        ShipmentDocumentTypes.GateOutCoupon => ShipmentActionCodes.PayMandatoryLocalCharges,
        ShipmentDocumentTypes.GateOutAdvanceReceipt => ShipmentActionCodes.PayMandatoryLocalCharges,
        ShipmentDocumentTypes.CollectReceipt => ShipmentActionCodes.DownloadCollectReceipt,
        ShipmentDocumentTypes.BlCopyValued => ShipmentActionCodes.RequestValuedBlCopy,
        ShipmentDocumentTypes.BlCopyNonValued => ShipmentActionCodes.RequestUnvaluedBlCopy,
        ShipmentDocumentTypes.ResponsibilityLetter => ShipmentActionCodes.GenerateResponsibilityLetter,
        ShipmentDocumentTypes.NoDebtCertificate => ShipmentActionCodes.DownloadNoDebtCertificate,
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Unknown document type.")
    };
}

/// <summary>Estado de un documento emitido. Una carta reemplazada por otra posterior queda <see cref="Superseded"/>.</summary>
public static class ShipmentDocumentStatus
{
    public const string Issued = "Issued";
    public const string Superseded = "Superseded";
    public const string Revoked = "Revoked";
}

/// <summary>Origen de la emisión: confirmación de un pago (cola NF-03), solicitud del cliente o datos de demostración.</summary>
public static class ShipmentDocumentOrigins
{
    public const string Payment = "Payment";
    public const string Request = "Request";
    public const string Seed = "Seed";
}

/// <summary>Eventos del registro de un documento (NF-14): emisión, descarga y envío por correo.</summary>
public static class ShipmentDocumentEventTypes
{
    public const string Issued = "Issued";
    public const string Downloaded = "Downloaded";
    public const string Sent = "Sent";
}

/// <summary>Canal por el que se entrega el documento: portal, correo, asistente (M10-04) o el sistema.</summary>
public static class DocumentChannels
{
    public const string Portal = "Portal";
    public const string Email = "Email";
    public const string Assistant = "Assistant";
    public const string System = "System";
}

/// <summary>Tipo de documento informado al firmante (CT-SIGN, <c>POST /sign</c>).</summary>
public static class SignatureDocumentTypes
{
    public const string TransshipmentCertificate = "TRANSSHIPMENT_CERTIFICATE";
    public const string NoDebtCertificate = "NO_DEBT_CERTIFICATE";

    public static string? For(string documentType) => documentType switch
    {
        ShipmentDocumentTypes.TransshipmentCertificate => TransshipmentCertificate,
        ShipmentDocumentTypes.NoDebtCertificate => NoDebtCertificate,
        _ => null
    };
}

/// <summary>Destinatario del certificado de transbordo (M6-01): el cliente, UMAR, o según la operación.</summary>
public static class TransshipmentRecipients
{
    /// <summary>Importación (CL-IMP-07): UMAR con copia al cliente; exportación (CL-EXP-08): el cliente.</summary>
    public const string Auto = "Auto";
    public const string Client = "Client";
    public const string Umar = "Umar";
}

/// <summary>Motivos que impiden emitir el certificado de libre deuda (M6-07).</summary>
public static class NoDebtBlockers
{
    public const string PendingCharges = "PENDING_CHARGES";
    public const string PendingDemurrage = "PENDING_DEMURRAGE";
    public const string PendingInvoices = "PENDING_INVOICES";
    public const string PendingFreight = "PENDING_FREIGHT";
    public const string AdvanceDemurrage = "ADVANCE_DEMURRAGE";
}

/// <summary>Términos vigentes de la carta de responsabilidad (M6-06). Cambiar el texto exige nueva versión.</summary>
public static class ResponsibilityLetterTerms
{
    public const string Version = "CARTA-RESP-2026-10";
    public const string Title = "Carta de responsabilidad";

    public const string Text =
        "El firmante, en representación de la organización indicada, declara que los datos ingresados son " +
        "verídicos y asume ante Hapag-Lloyd la responsabilidad por la carga amparada en el BL individualizado, " +
        "incluidos los cargos, demoras y perjuicios que se originen por su retiro y manipulación, liberando a " +
        "Hapag-Lloyd de toda responsabilidad frente al consignatario final y a terceros.";
}
