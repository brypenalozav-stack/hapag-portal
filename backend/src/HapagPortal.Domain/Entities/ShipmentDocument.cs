using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Documento emitido por el portal y publicado en el repositorio del embarque (M6-09): certificado de
/// transbordo, cupón de Gate Out, comprobante Collect, copias del BL, carta de responsabilidad y CLD.
/// Queda asociado al BL y a las unidades que lo originaron, con su firma (M6-01, M6-07), la huella del
/// contenido y la organización para la que se emitió. El contenido vive en el almacenamiento
/// (<see cref="StorageKey"/>); <see cref="TemplateJson"/> conserva los datos con que se generó, de modo que
/// un documento sembrado sin archivo se materializa en su primera descarga. No se elimina: se conserva
/// hasta <see cref="RetainUntil"/> (NF-16).
/// </summary>
public sealed class ShipmentDocument : BaseAuditableEntity
{
    public required string DocumentType { get; set; }
    public required string DocumentNumber { get; set; }
    public required string Status { get; set; }

    public Guid BillOfLadingId { get; set; }
    public required string BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public required string Country { get; set; }

    /// <summary>Unidades (contenedores) del documento, separadas por coma.</summary>
    public string? ContainerNumbers { get; set; }

    public DateTime IssuedAt { get; set; }

    /// <summary>Organización para la que se emitió (pagadora o solicitante).</summary>
    public Guid? IssuedForOrganizationId { get; set; }
    public Guid? IssuedByUserId { get; set; }
    public string? IssuedByEmail { get; set; }

    /// <summary>NF-14: mandante y acceso otorgado bajo el que se solicitó o pagó.</summary>
    public Guid? OnBehalfOfOrganizationId { get; set; }
    public Guid? AccessGrantId { get; set; }

    /// <summary>Pago (<c>ShipmentDocumentOrigins.Payment</c>), solicitud del cliente o semilla.</summary>
    public required string Origin { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? PaymentDetailId { get; set; }

    /// <summary>Clave de idempotencia de la emisión automática: un mismo ítem pagado emite un solo documento.</summary>
    public string? GenerationKey { get; set; }

    public string? StorageKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>SHA-256 (hex) del contenido almacenado, firmado si corresponde.</summary>
    public string? ContentHash { get; set; }

    /// <summary>Código impreso en el documento para verificar su emisión.</summary>
    public required string VerificationCode { get; set; }

    // Firma electrónica (CT-SIGN) de los documentos firmados.
    public string? SignatureId { get; set; }
    public string? SignatureProvider { get; set; }
    public string? SignatureLevel { get; set; }
    public DateTime? SignedAt { get; set; }

    /// <summary>Modelo del documento (JSON) con que se generó el PDF.</summary>
    public required string TemplateJson { get; set; }

    /// <summary>Destinatarios del envío automático (M6-01), separados por coma; nulo si no se envía.</summary>
    public string? RecipientEmails { get; set; }
    public DateTime? DeliveredAt { get; set; }

    /// <summary>Carta de responsabilidad (M6-06): versión de los términos aceptados y cuándo.</summary>
    public string? TermsVersion { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }

    /// <summary>Vigencia del documento (la carta deja de cumplir M4-04 al vencer); nula = sin vencimiento.</summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>Conservación mínima (NF-16): el documento y su registro permanecen consultables hasta esta fecha.</summary>
    public DateTime RetainUntil { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
    public ICollection<ShipmentDocumentEvent> Events { get; set; } = [];
}
