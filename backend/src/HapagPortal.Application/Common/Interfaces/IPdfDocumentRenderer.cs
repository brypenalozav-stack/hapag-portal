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
/// Los miembros opcionales posteriores (<c>StatusLabel</c>, <c>StatusTone</c>, <c>Highlight</c>) se agregaron
/// después: los modelos guardados sin ellos se siguen generando igual.
/// </summary>
/// <param name="StatusLabel">Estado legible impreso como insignia junto al título (p. ej. «Pagado»).</param>
/// <param name="StatusTone">Tono de la insignia: uno de <see cref="PdfTones"/>; vacío = neutro.</param>
/// <param name="Highlight">Dato principal destacado en un recuadro bajo el título (p. ej. el monto a depositar).</param>
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
    string? QrPayload = null,
    string? StatusLabel = null,
    string? StatusTone = null,
    PdfHighlight? Highlight = null);

public sealed record PdfField(string Label, string? Value);

/// <summary>Recuadro destacado: etiqueta pequeña, valor grande y una leyenda opcional, en el tono indicado.</summary>
public sealed record PdfHighlight(string Label, string Value, string? Caption = null, string? Tone = null);

/// <summary>Tonos de la insignia de estado, del recuadro destacado y de las notas.</summary>
public static class PdfTones
{
    public const string Neutral = "Neutral";
    public const string Info = "Info";
    public const string Success = "Success";
    public const string Warning = "Warning";
    public const string Danger = "Danger";
}

/// <summary>
/// Sección con campos, una tabla, totales, párrafos, pasos numerados y una nota destacada, en ese orden.
/// <c>Totals</c>: recuadro alineado a la derecha bajo la tabla; la última fila (el total) va destacada.
/// <c>Steps</c>: lista numerada. <c>Note</c>: texto en un recuadro sombreado del tono <c>NoteTone</c>.
/// </summary>
public sealed record PdfSection(
    string Heading,
    IReadOnlyList<PdfField>? Fields = null,
    PdfTable? Table = null,
    IReadOnlyList<string>? Paragraphs = null,
    IReadOnlyList<PdfField>? Totals = null,
    IReadOnlyList<string>? Steps = null,
    string? Note = null,
    string? NoteTone = null);

/// <summary>
/// Tabla simple. <c>NumericColumns</c>: índices de columnas alineadas a la derecha (montos).
/// <c>ColumnWidths</c>: pesos relativos de cada columna (se escalan al ancho de la página); sin ellos, el ancho se
/// calcula según el contenido para que ningún valor (p. ej. un número de BL) invada la columna vecina.
/// Una celda con saltos de línea (<c>\n</c>) imprime la primera línea normal y las siguientes como texto secundario.
/// </summary>
public sealed record PdfTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<int>? NumericColumns = null,
    IReadOnlyList<double>? ColumnWidths = null);
