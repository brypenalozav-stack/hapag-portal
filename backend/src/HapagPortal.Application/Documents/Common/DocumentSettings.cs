namespace HapagPortal.Application.Documents.Common;

using HapagPortal.Domain.Constants;

/// <summary>
/// Configuración de la generación documental (sección <c>Documents</c>). <see cref="RetentionYears"/> es
/// el plazo de conservación de NF-16: provisorio hasta que se valide antes del Go Live con los requisitos
/// legales, tributarios y de auditoría (el Código Tributario chileno exige al menos seis años para los
/// respaldos de documentos tributarios). Ningún proceso elimina documentos: el plazo se registra en cada
/// documento (<c>RetainUntil</c>) para la política que se apruebe.
/// </summary>
public sealed class DocumentSettings
{
    public const string SectionName = "Documents";

    /// <summary>Contenedor del almacenamiento (CT-STORAGE) donde se guardan los PDF.</summary>
    public string StorageContainer { get; set; } = "shipment-documents";

    /// <summary>Años de conservación de documentos y registros (NF-16).</summary>
    public int RetentionYears { get; set; } = 10;

    /// <summary>Destino del certificado de transbordo (M6-01): <c>Auto</c>, <c>Client</c> o <c>Umar</c>.</summary>
    public string TransshipmentRecipient { get; set; } = TransshipmentRecipients.Auto;

    /// <summary>Correo de UMAR para los certificados de transbordo de importación; vacío = solo el cliente.</summary>
    public string UmarEmail { get; set; } = string.Empty;

    /// <summary>Vigencia de la carta de responsabilidad en días (M6-06); 0 = sin vencimiento.</summary>
    public int ResponsibilityLetterValidityDays { get; set; }

    /// <summary>
    /// Cobro del certificado de flete (M6-02): <c>Free</c> (primera entrega de Fase 2, sin pago ni carro). <c>Paid</c>
    /// queda reservado para el flujo de cobro en BOB, aún no definido ni validado: el arranque falla si se configura.
    /// </summary>
    public string FreightCertificateMode { get; set; } = FreightCertificateModes.Free;

    /// <summary>
    /// Vínculo de la carta de liberación con el TATC (M6-08, M2-09): el TATC se consulta y registra siempre al enviar y
    /// al aprobar; con <c>true</c>, además, la aprobación exige el TATC emitido de todas las unidades seleccionadas (regla
    /// a validar con el área legal; por defecto solo informativa).
    /// </summary>
    public bool ReleaseLetterRequiresIssuedTatc { get; set; }

    /// <summary>Razón social del emisor por país, impresa en el encabezado de cada documento.</summary>
    public string IssuerChile { get; set; } = "Hapag-Lloyd Chile SpA";
    public string IssuerBolivia { get; set; } = "Hapag-Lloyd Bolivia S.R.L.";

    public string IssuerFor(string country) => country == CountryCodes.Bolivia ? IssuerBolivia : IssuerChile;
}
