namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Genera el PDF de un documento del portal a partir de su modelo (plantilla común de Hapag-Lloyd:
/// encabezado con el emisor, número, fecha de emisión en el huso del país, referencias del embarque,
/// secciones y código de verificación). La implementación usa PDFsharp/MigraDoc (MIT).
/// </summary>
public interface IPdfDocumentRenderer
{
    byte[] Render(PdfDocumentModel document);
}

/// <summary>
/// Modelo de un documento. Es serializable (JSON) para guardar con el registro los datos con que se
/// generó y poder volver a generarlo. <c>IssuedAt</c> es UTC y se presenta en <c>TimeZoneId</c> (NF-22).
/// <c>QrPayload</c>: texto que se imprime como código QR junto a las referencias (p. ej. el comprobante de TATC).
/// </summary>
public sealed record PdfDocumentModel(
    string Title,
    string? Subtitle,
    string Issuer,
    string IssuerDetail,
    string DocumentNumber,
    DateTime IssuedAt,
    string TimeZoneId,
    IReadOnlyList<PdfField> References,
    IReadOnlyList<PdfSection> Sections,
    string? VerificationCode,
    string? SignatureNote,
    string? Footer,
    string? QrPayload = null);

public sealed record PdfField(string Label, string? Value);

/// <summary>Sección con campos, una tabla y/o párrafos, en ese orden.</summary>
public sealed record PdfSection(
    string Heading,
    IReadOnlyList<PdfField>? Fields = null,
    PdfTable? Table = null,
    IReadOnlyList<string>? Paragraphs = null);

/// <summary>Tabla simple. <c>NumericColumns</c>: índices de columnas alineadas a la derecha (montos).</summary>
public sealed record PdfTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<int>? NumericColumns = null);
