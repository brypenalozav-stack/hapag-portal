namespace HapagPortal.Domain.Constants;

/// <summary>Documento de transporte del embarque (M2-02): BL original, Sea Waybill o BL electrónico.</summary>
public static class TransportDocumentTypes
{
    public const string Bl = "BL";
    public const string Swb = "SWB";
    public const string Ebl = "EBL";

    public static readonly string[] All = [Bl, Swb, Ebl];
}

/// <summary>Plataformas de BL electrónico informadas por el origen (M2-02).</summary>
public static class EblPlatforms
{
    public const string Wave = "WAVE";
}

/// <summary>
/// Estado de emisión del documento de transporte en el portal (M2-02, CL-IMP-14, BO-IMP-14). Se obtiene
/// del código del origen con <c>BlIssuanceMapper</c>; un código desconocido queda en <see cref="Unknown"/>
/// (el portal no infiere un estado que el origen no informó).
/// </summary>
public static class BlIssuanceStatuses
{
    /// <summary>Sin emitir todavía (borrador o pendiente de emisión).</summary>
    public const string Pending = "Pending";

    /// <summary>Emitido en origen (BL original o SWB) o publicado en la plataforma (EBL).</summary>
    public const string Issued = "Issued";

    /// <summary>Emisión autorizada en destino: los originales se emiten en el país de destino.</summary>
    public const string AuthorizedAtDestination = "AuthorizedAtDestination";

    /// <summary>Emitido en destino.</summary>
    public const string IssuedAtDestination = "IssuedAtDestination";

    /// <summary>EBL transferido a otro tenedor en la plataforma.</summary>
    public const string Transferred = "Transferred";

    /// <summary>Originales o EBL entregados (surrender) a la línea.</summary>
    public const string Surrendered = "Surrendered";

    /// <summary>Liberado por telex release.</summary>
    public const string TelexReleased = "TelexReleased";

    /// <summary>Documento anulado en el origen.</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>El origen informó un código que el portal no reconoce.</summary>
    public const string Unknown = "Unknown";

    public static readonly string[] All =
    [
        Pending, Issued, AuthorizedAtDestination, IssuedAtDestination, Transferred, Surrendered, TelexReleased,
        Cancelled, Unknown
    ];
}

/// <summary>Códigos de estado de emisión propuestos en CT-FIS (<c>issuanceStatus</c>).</summary>
public static class BlIssuanceSourceStatuses
{
    public const string NotIssued = "NOT_ISSUED";
    public const string Draft = "DRAFT";
    public const string Issued = "ISSUED";
    public const string DestinationIssuanceAuthorized = "DESTINATION_ISSUANCE_AUTHORIZED";
    public const string IssuedAtDestination = "ISSUED_AT_DESTINATION";
    public const string Transferred = "TRANSFERRED";
    public const string Surrendered = "SURRENDERED";
    public const string TelexReleased = "TELEX_RELEASED";
    public const string Voided = "VOIDED";
}

/// <summary>Motivo de la decisión de publicación de un BL (M2-01), visible solo para el administrador interno.</summary>
public static class ShipmentPublicationReasons
{
    /// <summary>Ninguna regla activa aplica al destino final: se publica.</summary>
    public const string NoRule = "NO_RULE";

    /// <summary>El destino final es el puerto de descarga: no requiere DIFU.</summary>
    public const string SameAsDischarge = "SAME_AS_DISCHARGE";

    /// <summary>El origen informa un DIFU asociado al destino final: se publica.</summary>
    public const string DifuAssociated = "DIFU_ASSOCIATED";

    /// <summary>La regla exige DIFU y el origen no lo informa: no se publica.</summary>
    public const string DifuMissing = "DIFU_MISSING";

    /// <summary>El DIFU informado está asociado a otra localidad: no se publica.</summary>
    public const string DifuOtherLocation = "DIFU_OTHER_LOCATION";
}

/// <summary>
/// Estados del TATC en el portal (M2-09), por contenedor y agregado por BL. Se obtienen del código del
/// sistema de TATC (CT-TATC) con <c>TatcStatusMapper</c>; ver la tabla en el contrato.
/// </summary>
public static class TatcStatuses
{
    /// <summary>Contenedor sin TATC emitido (ver motivos pendientes).</summary>
    public const string NotIssued = "NotIssued";

    /// <summary>Pre-TATC generado, a la espera de la emisión definitiva.</summary>
    public const string PreTatc = "PreTatc";

    /// <summary>TATC emitido.</summary>
    public const string Issued = "Issued";

    /// <summary>TATC anulado.</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>Código no reconocido por el portal.</summary>
    public const string Unknown = "Unknown";

    /// <summary>Agregado por BL: algunos contenedores con TATC emitido y otros no.</summary>
    public const string PartiallyIssued = "PartiallyIssued";

    /// <summary>Agregado por BL: el sistema de TATC no tiene registro del BL.</summary>
    public const string NotRegistered = "NotRegistered";
}

/// <summary>Códigos del sistema de TATC (CT-TATC <c>ContainerTatc.status</c>).</summary>
public static class TatcSourceStatuses
{
    public const string NotIssued = "NOT_ISSUED";
    public const string PreTatc = "PRE_TATC";
    public const string Issued = "ISSUED";
    public const string Cancelled = "CANCELLED";
}

/// <summary>Motivos por los que el TATC aún no se emite (CT-TATC <c>pendingReasons</c>).</summary>
public static class TatcPendingReasons
{
    public const string PaymentPending = "PAYMENT_PENDING";
    public const string MhdPending = "MHD_PENDING";
    public const string DocumentPending = "DOCUMENT_PENDING";
    public const string Other = "OTHER";
}

/// <summary>Estado de una solicitud masiva de TATC (M2-09).</summary>
public static class TatcBatchStatus
{
    public const string Completed = "Completed";
    public const string CompletedWithErrors = "CompletedWithErrors";
    public const string Failed = "Failed";
}

/// <summary>Resultado de una línea de la solicitud masiva de TATC.</summary>
public static class TatcBatchItemStatus
{
    /// <summary>El sistema de TATC aceptó generar el TATC del BL.</summary>
    public const string Accepted = "Accepted";

    /// <summary>El sistema de TATC rechazó la línea (ver <c>reasonCode</c>).</summary>
    public const string Rejected = "Rejected";

    /// <summary>La línea no se envió: no pasó la validación del portal o el sistema no respondió.</summary>
    public const string Failed = "Failed";
}

/// <summary>Motivos de una línea de la solicitud masiva de TATC no aceptada.</summary>
public static class TatcBatchReasons
{
    public const string NotFound = "NOT_FOUND";
    public const string NoPermission = "NO_PERMISSION";
    public const string NotImport = "NOT_IMPORT";
    public const string OtherLocation = "OTHER_LOCATION";
    public const string OtherCountry = "OTHER_COUNTRY";
    public const string Duplicate = "DUPLICATE";
    public const string AlreadyIssued = "ALREADY_ISSUED";
    public const string SourceUnavailable = "SOURCE_UNAVAILABLE";
}
