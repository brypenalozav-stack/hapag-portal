namespace HapagPortal.Application.Documents.Common;

/// <summary>Documento del repositorio del embarque (M6-09).</summary>
public sealed record ShipmentDocumentDto(
    Guid Id,
    string DocumentType,
    string DocumentNumber,
    string Status,
    Guid BillOfLadingId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    IReadOnlyList<string> ContainerNumbers,
    DateTime IssuedAt,
    string Origin,
    Guid? IssuedForOrganizationId,
    string? IssuedForOrganizationName,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? ContentHash,
    string VerificationCode,
    bool Signed,
    string? SignatureId,
    string? SignatureProvider,
    string? SignatureLevel,
    DateTime? SignedAt,
    Guid? PaymentId,
    IReadOnlyList<string> RecipientEmails,
    DateTime? DeliveredAt,
    string? TermsVersion,
    DateTime? TermsAcceptedAt,
    DateTime? ValidUntil,
    DateTime RetainUntil);

/// <summary>Recibo de pago o factura del BL publicados por otros módulos (M7-01, M7-02) y descargables por su ruta.</summary>
public sealed record RelatedDocumentDto(
    string Kind,
    Guid Id,
    string Number,
    DateTime IssuedAt,
    string? Description,
    string DownloadPath);

/// <summary>Estado de la carta de responsabilidad del usuario sobre el BL (M4-04, M6-06).</summary>
public sealed record ResponsibilityLetterStateDto(bool Required, string Status, bool BlocksProcess);

/// <summary>Qué documentos puede solicitar el usuario sobre el BL (M1-11 y perfil que opera).</summary>
public sealed record DocumentActionsDto(
    bool CanRequestValuedCopy,
    bool CanRequestNonValuedCopy,
    bool CanIssueResponsibilityLetter,
    bool CanRequestNoDebtCertificate,
    bool CanRequestTransshipmentCertificate);

public sealed record ShipmentDocumentsDto(
    Guid BlId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    string TimeZone,
    IReadOnlyList<ShipmentDocumentDto> Documents,
    IReadOnlyList<RelatedDocumentDto> Related,
    DocumentActionsDto Actions,
    ResponsibilityLetterStateDto? ResponsibilityLetter);

public sealed record DocumentFileDto(byte[] Content, string ContentType, string FileName);

/// <summary>Documento emitido o reenviado, con los correos a los que se envió.</summary>
public sealed record DocumentDeliveryDto(ShipmentDocumentDto Document, IReadOnlyList<string> SentTo);

/// <summary>Motivo que impide el certificado de libre deuda, con las referencias que lo componen (M6-07).</summary>
public sealed record NoDebtBlockerDto(string Code, IReadOnlyList<string> References, IReadOnlyList<NoDebtAmountDto> Amounts);

public sealed record NoDebtAmountDto(string Currency, decimal Total);

public sealed record NoDebtEligibilityDto(
    Guid BlId,
    string BlNumber,
    string Country,
    bool Applicable,
    bool Eligible,
    bool CanRequest,
    IReadOnlyList<NoDebtBlockerDto> Blockers,
    DateTime EvaluatedAt);

public sealed record ResponsibilityLetterTermsDto(string Version, string Title, string Text);

/// <summary>Cargo del servicio de certificado de transbordo listo para el carro (M6-01).</summary>
public sealed record TransshipmentRequestDto(
    Guid ChargeId,
    Guid BlId,
    string BlNumber,
    string ConceptCode,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    Guid? DocumentId);
