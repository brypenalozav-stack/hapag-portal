using System.Text.RegularExpressions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using QRCoder;

namespace HapagPortal.Infrastructure.Documents;

/// <summary>
/// Plantilla común de los documentos del portal con PDFsharp/MigraDoc 6 (MIT): encabezado con el emisor
/// (Hapag-Lloyd) y, a la derecha, el tipo, número y fecha de emisión en el huso del país (NF-22); título con la
/// insignia de estado; recuadro destacado; ficha de referencias; secciones (campos, tablas con anchos según el
/// contenido, totales, párrafos, pasos y notas); nota de firma; y pie con el código de verificación, la nota
/// legal y la numeración de páginas. La fuente es Liberation Sans incrustada (<see cref="EmbeddedFontResolver"/>),
/// igual en Windows y Linux.
/// </summary>
public sealed partial class MigraDocPdfRenderer : IPdfDocumentRenderer
{
    private const double ContentWidthCm = 17;
    private const double TableFontSize = 8;
    private const double SecondaryFontSize = 7;
    private const double CellPaddingCm = 0.36;
    private const double PointsPerCm = 72 / 2.54;

    private static readonly Lock FontGate = new();
    private static readonly EmbeddedFontResolver Fonts = new();

    private static readonly Color Navy = new(0, 43, 73);
    private static readonly Color Orange = new(245, 90, 0);
    private static readonly Color Ink = new(33, 37, 41);
    private static readonly Color Muted = new(98, 106, 116);
    private static readonly Color Rule = new(214, 220, 226);
    private static readonly Color LabelShade = new(241, 244, 247);
    private static readonly Color Zebra = new(247, 249, 251);

    public MigraDocPdfRenderer() => EnsureFonts();

    public byte[] Render(PdfDocumentModel model)
    {
        var document = new Document();
        document.Info.Title = $"{model.Title} {model.DocumentNumber}";
        document.Info.Author = model.Issuer;
        document.Info.Subject = model.Subtitle ?? model.Title;

        DefineStyles(document);

        var section = document.AddSection();
        section.PageSetup = document.DefaultPageSetup.Clone();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(3.1);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(2.4);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2);
        section.PageSetup.HeaderDistance = Unit.FromCentimeter(1.1);
        section.PageSetup.FooterDistance = Unit.FromCentimeter(0.9);

        using var measure = TextMeasure.Create();

        AddHeader(section, model);
        AddFooter(section, model);
        AddTitle(section, model, measure);
        AddHighlight(section, model.Highlight);
        AddQrCode(section, model.QrPayload);
        AddFactsCard(section, model.References);

        foreach (var part in model.Sections)
            AddSection(section, part, measure);

        if (!string.IsNullOrWhiteSpace(model.SignatureNote))
        {
            var note = section.AddParagraph(model.SignatureNote);
            note.Format.SpaceBefore = Unit.FromPoint(14);
            note.Format.Font.Italic = true;
            note.Format.Font.Color = Muted;
        }

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        renderer.PdfDocument.Info.CreationDate = model.IssuedAt;

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }

    /// <summary>El resolvedor de fuentes es global en PDFsharp y solo puede fijarse una vez por proceso.</summary>
    private static void EnsureFonts()
    {
        lock (FontGate)
        {
            if (GlobalFontSettings.FontResolver is not EmbeddedFontResolver)
                GlobalFontSettings.FontResolver = Fonts;
        }
    }

    private static void DefineStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = EmbeddedFontResolver.FamilyName;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var heading = document.Styles.AddStyle("SectionHeading", StyleNames.Normal);
        heading.Font.Size = 10.5;
        heading.Font.Bold = true;
        heading.Font.Color = Navy;
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(14);
        heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        heading.ParagraphFormat.KeepWithNext = true;
        heading.ParagraphFormat.Borders.Bottom.Width = Unit.FromPoint(0.75);
        heading.ParagraphFormat.Borders.Bottom.Color = Rule;
        heading.ParagraphFormat.Borders.Left.Width = Unit.FromPoint(3);
        heading.ParagraphFormat.Borders.Left.Color = Orange;
        heading.ParagraphFormat.Borders.DistanceFromLeft = Unit.FromPoint(5);
        heading.ParagraphFormat.Borders.DistanceFromBottom = Unit.FromPoint(2);
    }

    // ---------------------------------------------------------------- encabezado y pie

    /// <summary>Emisor a la izquierda; tipo de documento, número y fecha de emisión a la derecha; filete naranja.</summary>
    private static void AddHeader(Section section, PdfDocumentModel model)
    {
        var header = section.Headers.Primary;
        var table = header.AddTable();
        table.Borders.Visible = false;
        table.LeftPadding = 0;
        table.RightPadding = 0;
        table.AddColumn(Unit.FromCentimeter(10));
        table.AddColumn(Unit.FromCentimeter(ContentWidthCm - 10));

        var row = table.AddRow();
        row.VerticalAlignment = VerticalAlignment.Bottom;

        var issuer = row.Cells[0].AddParagraph();
        issuer.AddFormattedText(model.Issuer, TextFormat.Bold);
        issuer.Format.Font.Size = 13;
        issuer.Format.Font.Color = Navy;

        var detail = row.Cells[0].AddParagraph(model.IssuerDetail);
        detail.Format.Font.Size = 8;
        detail.Format.Font.Color = Muted;

        var kind = row.Cells[1].AddParagraph(model.Title.ToUpperInvariant());
        kind.Format.Alignment = ParagraphAlignment.Right;
        kind.Format.Font.Size = 7;
        kind.Format.Font.Bold = true;
        kind.Format.Font.Color = Orange;

        var number = row.Cells[1].AddParagraph();
        number.Format.Alignment = ParagraphAlignment.Right;
        number.Format.Font.Size = 9.5;
        number.AddText("N° ");
        number.AddFormattedText(model.DocumentNumber, TextFormat.Bold);

        var issued = row.Cells[1].AddParagraph($"Emitido: {IssuedAt(model)}");
        issued.Format.Alignment = ParagraphAlignment.Right;
        issued.Format.Font.Size = 7.5;
        issued.Format.Font.Color = Muted;

        var rule = header.AddParagraph();
        rule.Format.Font.Size = 2;
        rule.Format.SpaceBefore = Unit.FromPoint(4);
        rule.Format.Borders.Bottom.Width = Unit.FromPoint(1.75);
        rule.Format.Borders.Bottom.Color = Orange;
    }

    /// <summary>Filete gris; código de verificación y nota legal a la izquierda; número y página a la derecha.</summary>
    private static void AddFooter(Section section, PdfDocumentModel model)
    {
        var footer = section.Footers.Primary;
        var table = footer.AddTable();
        table.Borders.Visible = false;
        table.LeftPadding = 0;
        table.RightPadding = 0;
        table.TopPadding = Unit.FromPoint(4);
        table.AddColumn(Unit.FromCentimeter(12.5));
        table.AddColumn(Unit.FromCentimeter(ContentWidthCm - 12.5));

        var row = table.AddRow();
        row.Borders.Top.Width = Unit.FromPoint(0.75);
        row.Borders.Top.Color = Rule;

        var left = row.Cells[0];
        if (!string.IsNullOrWhiteSpace(model.VerificationCode))
        {
            var verification = left.AddParagraph();
            verification.Format.Font.Size = 7.5;
            verification.AddText("Código de verificación: ");
            verification.AddFormattedText(model.VerificationCode, TextFormat.Bold);
        }

        if (!string.IsNullOrWhiteSpace(model.Footer))
        {
            var text = left.AddParagraph(model.Footer);
            text.Format.Font.Size = 6.5;
            text.Format.Font.Color = Muted;
        }

        var pages = row.Cells[1].AddParagraph();
        pages.Format.Alignment = ParagraphAlignment.Right;
        pages.Format.Font.Size = 7;
        pages.Format.Font.Color = Muted;
        pages.AddText(model.DocumentNumber);
        pages.AddLineBreak();
        pages.AddText("Página ");
        pages.AddPageField();
        pages.AddText(" de ");
        pages.AddNumPagesField();
    }

    private static string IssuedAt(PdfDocumentModel model)
    {
        var country = model.TimeZoneId == BusinessCalendar.BoliviaTimeZone ? CountryCodes.Bolivia : CountryCodes.Chile;
        return $"{BusinessCalendar.ToLocal(country, model.IssuedAt):dd-MM-yyyy HH:mm} ({model.TimeZoneId})";
    }

    // ---------------------------------------------------------------- título, estado y destacado

    /// <summary>Título con la insignia de estado a la derecha (si hay) y el subtítulo debajo.</summary>
    private static void AddTitle(Section section, PdfDocumentModel model, TextMeasure measure)
    {
        var table = section.AddTable();
        table.Borders.Visible = false;
        table.LeftPadding = 0;
        table.RightPadding = 0;

        var hasStatus = !string.IsNullOrWhiteSpace(model.StatusLabel);
        var badgeCm = hasStatus
            ? Math.Min(7, measure.WidthCm(model.StatusLabel!, 8.5, bold: true) + 0.9)
            : 0;
        var gapCm = hasStatus ? 0.4 : 0;
        table.AddColumn(Unit.FromCentimeter(ContentWidthCm - badgeCm - gapCm));
        if (hasStatus)
        {
            table.AddColumn(Unit.FromCentimeter(gapCm));
            table.AddColumn(Unit.FromCentimeter(badgeCm));
        }

        var row = table.AddRow();
        row.VerticalAlignment = VerticalAlignment.Center;

        var title = row.Cells[0].AddParagraph();
        title.AddFormattedText(model.Title, TextFormat.Bold);
        title.Format.Font.Size = 17;
        title.Format.Font.Color = Navy;

        if (hasStatus)
        {
            var (fore, back) = ToneColors(model.StatusTone);
            var badge = row.Cells[2];
            badge.Shading.Color = back;
            badge.Borders.Width = Unit.FromPoint(0.75);
            badge.Borders.Color = fore;
            badge.VerticalAlignment = VerticalAlignment.Center;
            var label = badge.AddParagraph(model.StatusLabel!);
            label.Format.Alignment = ParagraphAlignment.Center;
            label.Format.Font.Size = 8.5;
            label.Format.Font.Bold = true;
            label.Format.Font.Color = fore;
            label.Format.SpaceBefore = Unit.FromPoint(3);
            label.Format.SpaceAfter = Unit.FromPoint(3);
        }

        if (!string.IsNullOrWhiteSpace(model.Subtitle))
        {
            var subtitle = section.AddParagraph(model.Subtitle);
            subtitle.Format.Font.Size = 10;
            subtitle.Format.Font.Color = Muted;
            subtitle.Format.SpaceBefore = Unit.FromPoint(3);
        }

        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(4);
    }

    /// <summary>Recuadro con borde izquierdo grueso: etiqueta, valor grande y leyenda a la derecha.</summary>
    private static void AddHighlight(Section section, PdfHighlight? highlight)
    {
        if (highlight is null || string.IsNullOrWhiteSpace(highlight.Value))
            return;

        var (fore, back) = ToneColors(highlight.Tone);
        var table = section.AddTable();
        table.Borders.Visible = false;
        Pad(table, 0.45);
        table.TopPadding = Unit.FromPoint(7);
        table.BottomPadding = Unit.FromPoint(7);
        var hasCaption = !string.IsNullOrWhiteSpace(highlight.Caption);
        // El borde izquierdo de 4 pt se dibuja por fuera de la columna: se descuenta para no pasar el margen derecho.
        var widthCm = ContentWidthCm - 4 / PointsPerCm;
        table.AddColumn(Unit.FromCentimeter(hasCaption ? 8 : widthCm));
        if (hasCaption)
            table.AddColumn(Unit.FromCentimeter(widthCm - 8));

        var row = table.AddRow();
        row.Shading.Color = back;
        row.VerticalAlignment = VerticalAlignment.Center;
        row.Cells[0].Borders.Left.Width = Unit.FromPoint(4);
        row.Cells[0].Borders.Left.Color = fore;

        var label = row.Cells[0].AddParagraph(highlight.Label.ToUpperInvariant());
        label.Format.Font.Size = 7.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Muted;

        var value = row.Cells[0].AddParagraph();
        value.AddFormattedText(highlight.Value, TextFormat.Bold);
        value.Format.Font.Size = 20;
        value.Format.Font.Color = Navy;
        value.Format.SpaceBefore = Unit.FromPoint(1);

        if (hasCaption)
        {
            var caption = row.Cells[1].AddParagraph(highlight.Caption!);
            caption.Format.Font.Size = 8.5;
            caption.Format.Font.Color = Ink;
        }

        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(2);
    }

    /// <summary>Código QR (QRCoder, MIT) alineado a la derecha, de 3,2 cm; sin texto no se dibuja.</summary>
    private static void AddQrCode(Section section, string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(8);

        var holder = section.AddParagraph();
        holder.Format.Alignment = ParagraphAlignment.Right;
        holder.Format.SpaceAfter = Unit.FromPoint(4);
        var image = holder.AddImage("base64:" + Convert.ToBase64String(png));
        image.Width = Unit.FromCentimeter(3.2);
        image.LockAspectRatio = true;
    }

    // ---------------------------------------------------------------- referencias y campos

    /// <summary>
    /// Ficha de referencias: cada dato con su etiqueta pequeña en gris sobre el valor en negrita; tres por fila si
    /// los valores son cortos, dos si alguno es largo.
    /// </summary>
    private static void AddFactsCard(Section section, IReadOnlyList<PdfField> fields)
    {
        if (fields.Count == 0)
            return;

        var perRow = fields.Count >= 3 && fields.All(f => (f.Value?.Length ?? 1) <= 26) ? 3 : 2;
        var table = section.AddTable();
        table.Borders.Visible = false;
        Pad(table, 0.3);
        table.TopPadding = Unit.FromPoint(4);
        table.BottomPadding = Unit.FromPoint(4);
        for (var i = 0; i < perRow; i++)
            table.AddColumn(Unit.FromCentimeter(ContentWidthCm / perRow));

        var rows = (fields.Count + perRow - 1) / perRow;
        for (var r = 0; r < rows; r++)
        {
            var row = table.AddRow();
            row.Shading.Color = LabelShade;
            if (r < rows - 1)
            {
                row.Borders.Bottom.Width = Unit.FromPoint(0.5);
                row.Borders.Bottom.Color = Rule;
            }

            for (var c = 0; c < perRow; c++)
            {
                var index = r * perRow + c;
                if (index >= fields.Count)
                    continue;

                var cell = row.Cells[c];
                var label = cell.AddParagraph(fields[index].Label);
                label.Format.Font.Size = 7;
                label.Format.Font.Color = Muted;

                var value = cell.AddParagraph();
                value.AddFormattedText(Display(fields[index].Value), TextFormat.Bold);
                value.Format.Font.Size = 9.5;
                value.Format.Font.Color = Ink;
            }
        }
    }

    /// <summary>Campos de una sección: etiqueta en gris a la izquierda y valor a la derecha, con filetes finos.</summary>
    private static void AddFieldList(Section section, IReadOnlyList<PdfField> fields)
    {
        var table = section.AddTable();
        table.Borders.Visible = false;
        Pad(table, 0.15);
        table.TopPadding = Unit.FromPoint(3);
        table.BottomPadding = Unit.FromPoint(3);
        table.AddColumn(Unit.FromCentimeter(5));
        table.AddColumn(Unit.FromCentimeter(ContentWidthCm - 5));

        for (var i = 0; i < fields.Count; i++)
        {
            var row = table.AddRow();
            if (i < fields.Count - 1)
            {
                row.Borders.Bottom.Width = Unit.FromPoint(0.5);
                row.Borders.Bottom.Color = Rule;
            }

            var label = row.Cells[0].AddParagraph(fields[i].Label);
            label.Format.Font.Color = Muted;
            AddMultiline(row.Cells[1], Display(fields[i].Value), 9.5, bold: true);
        }
    }

    /// <summary>
    /// Relleno horizontal de las celdas. Sin sangría explícita MigraDoc corre la tabla a la izquierda en el relleno para
    /// alinear el texto con los párrafos; la sangría cero deja el fondo y los filetes dentro de los márgenes.
    /// </summary>
    private static void Pad(Table table, double paddingCm)
    {
        table.LeftPadding = Unit.FromCentimeter(paddingCm);
        table.RightPadding = Unit.FromCentimeter(paddingCm);
        table.Rows.LeftIndent = 0;
    }

    /// <summary>Bordes laterales del color del fondo: evitan la línea blanca entre celdas sombreadas.</summary>
    private static void CloseGaps(Row row, Color color)
    {
        for (var i = 0; i < row.Cells.Count; i++)
        {
            var borders = row.Cells[i].Borders;
            borders.Left.Visible = true;
            borders.Left.Width = Unit.FromPoint(0.75);
            borders.Left.Color = color;
            borders.Right.Visible = true;
            borders.Right.Width = Unit.FromPoint(0.75);
            borders.Right.Color = color;
        }
    }

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    // ---------------------------------------------------------------- secciones

    private static void AddSection(Section section, PdfSection part, TextMeasure measure)
    {
        section.AddParagraph(part.Heading, "SectionHeading");

        if (part.Fields is { Count: > 0 } fields)
            AddFieldList(section, fields);

        if (part.Table is { } table)
        {
            if (part.Fields is { Count: > 0 })
                section.AddParagraph().Format.SpaceBefore = Unit.FromPoint(4);
            AddTable(section, table, measure);
        }

        if (part.Totals is { Count: > 0 } totals)
            AddTotals(section, totals);

        foreach (var text in part.Paragraphs ?? [])
        {
            var paragraph = section.AddParagraph(text);
            paragraph.Format.SpaceAfter = Unit.FromPoint(4);
            paragraph.Format.Alignment = ParagraphAlignment.Justify;
        }

        if (part.Steps is { Count: > 0 } steps)
            AddSteps(section, steps);

        if (!string.IsNullOrWhiteSpace(part.Note))
            AddNote(section, part.Note!, part.NoteTone);
    }

    /// <summary>Recuadro de totales a la derecha: filas con etiqueta y monto; la última, en azul y negrita.</summary>
    private static void AddTotals(Section section, IReadOnlyList<PdfField> totals)
    {
        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(2);

        var table = section.AddTable();
        table.Borders.Visible = false;
        Pad(table, 0.3);
        table.Rows.LeftIndent = Unit.FromCentimeter(ContentWidthCm - 8);
        table.TopPadding = Unit.FromPoint(3);
        table.BottomPadding = Unit.FromPoint(3);
        table.AddColumn(Unit.FromCentimeter(3.6));
        table.AddColumn(Unit.FromCentimeter(4.4));

        for (var i = 0; i < totals.Count; i++)
        {
            var last = i == totals.Count - 1;
            var row = table.AddRow();
            row.KeepWith = totals.Count - 1 - i;
            if (last)
            {
                row.Shading.Color = Navy;
                CloseGaps(row, Navy);
                row.TopPadding = Unit.FromPoint(5);
                row.BottomPadding = Unit.FromPoint(5);
            }
            else
            {
                row.Borders.Bottom.Width = Unit.FromPoint(0.5);
                row.Borders.Bottom.Color = Rule;
            }

            var label = row.Cells[0].AddParagraph(totals[i].Label);
            var value = row.Cells[1].AddParagraph(Display(totals[i].Value));
            value.Format.Alignment = ParagraphAlignment.Right;
            if (last)
            {
                foreach (var paragraph in new[] { label, value })
                {
                    paragraph.Format.Font.Bold = true;
                    paragraph.Format.Font.Color = Colors.White;
                }

                label.Format.Font.Size = 10;
                value.Format.Font.Size = 11.5;
            }
            else
            {
                label.Format.Font.Color = Muted;
            }
        }
    }

    /// <summary>Pasos numerados con sangría francesa.</summary>
    private static void AddSteps(Section section, IReadOnlyList<string> steps)
    {
        var table = section.AddTable();
        table.Borders.Visible = false;
        table.LeftPadding = 0;
        table.RightPadding = Unit.FromCentimeter(0.1);
        table.TopPadding = Unit.FromPoint(2);
        table.BottomPadding = Unit.FromPoint(2);
        table.AddColumn(Unit.FromCentimeter(0.8));
        table.AddColumn(Unit.FromCentimeter(ContentWidthCm - 0.8));

        for (var i = 0; i < steps.Count; i++)
        {
            var row = table.AddRow();
            var number = row.Cells[0].AddParagraph();
            number.AddFormattedText($"{i + 1}.", TextFormat.Bold);
            number.Format.Font.Color = Orange;
            number.Format.Alignment = ParagraphAlignment.Right;
            number.Format.RightIndent = Unit.FromPoint(4);
            row.Cells[1].AddParagraph(steps[i]);
        }
    }

    private static void AddNote(Section section, string note, string? tone)
    {
        var (fore, back) = ToneColors(tone ?? PdfTones.Info);
        var paragraph = section.AddParagraph(note);
        paragraph.Format.SpaceBefore = Unit.FromPoint(6);
        paragraph.Format.Shading.Color = back;
        paragraph.Format.Borders.Left.Width = Unit.FromPoint(3);
        paragraph.Format.Borders.Left.Color = fore;
        paragraph.Format.Borders.DistanceFromLeft = Unit.FromPoint(6);
        paragraph.Format.Borders.DistanceFromTop = Unit.FromPoint(4);
        paragraph.Format.Borders.DistanceFromBottom = Unit.FromPoint(4);
        paragraph.Format.Borders.DistanceFromRight = Unit.FromPoint(6);
        paragraph.Format.LeftIndent = Unit.FromPoint(9);
        paragraph.Format.RightIndent = Unit.FromPoint(6);
        paragraph.Format.Font.Size = 8.5;
    }

    // ---------------------------------------------------------------- tablas

    private static void AddTable(Section section, PdfTable data, TextMeasure measure)
    {
        if (data.Headers.Count == 0)
            return;

        var columns = data.Headers.Count;
        var numeric = (data.NumericColumns ?? DetectNumeric(data)).ToHashSet();
        var widths = ColumnWidthsCm(data, measure);

        var table = section.AddTable();
        table.Borders.Visible = false;
        table.Format.Font.Size = TableFontSize;
        Pad(table, CellPaddingCm / 2);
        table.TopPadding = Unit.FromPoint(3.5);
        table.BottomPadding = Unit.FromPoint(3.5);
        foreach (var width in widths)
            table.AddColumn(Unit.FromCentimeter(width));

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Navy;
        CloseGaps(header, Navy);
        header.VerticalAlignment = VerticalAlignment.Center;
        for (var i = 0; i < columns; i++)
        {
            var paragraph = header.Cells[i].AddParagraph();
            AddWrapped(paragraph, data.Headers[i], widths[i], TableFontSize, bold: true, measure);
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Color = Colors.White;
            if (numeric.Contains(i))
                paragraph.Format.Alignment = ParagraphAlignment.Right;
        }

        for (var r = 0; r < data.Rows.Count; r++)
        {
            var values = data.Rows[r];
            var row = table.AddRow();
            row.Borders.Bottom.Width = Unit.FromPoint(0.5);
            row.Borders.Bottom.Color = Rule;
            if (r % 2 == 1)
                row.Shading.Color = Zebra;

            for (var i = 0; i < columns; i++)
            {
                var text = i < values.Count ? values[i] ?? string.Empty : string.Empty;
                var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
                for (var l = 0; l < lines.Length; l++)
                {
                    var size = l == 0 ? TableFontSize : SecondaryFontSize;
                    var paragraph = row.Cells[i].AddParagraph();
                    AddWrapped(paragraph, lines[l], widths[i], size, bold: false, measure);
                    paragraph.Format.Font.Size = size;
                    if (l > 0)
                        paragraph.Format.Font.Color = Muted;
                    if (numeric.Contains(i))
                        paragraph.Format.Alignment = ParagraphAlignment.Right;
                }
            }
        }
    }

    /// <summary>Anchos (cm) con que se imprimiría la tabla; sirve para pruebas y diagnóstico.</summary>
    public static IReadOnlyList<double> ColumnWidthsCm(PdfTable data)
    {
        EnsureFonts();
        using var measure = TextMeasure.Create();
        return ColumnWidthsCm(data, measure);
    }

    /// <summary>
    /// Anchos en cm que suman el ancho útil. Con <see cref="PdfTable.ColumnWidths"/> se escalan esos pesos; sin ellos,
    /// cada columna recibe al menos el ancho de su palabra más larga (no se puede cortar) y el resto se reparte según
    /// el largo del contenido, con tope para que una columna de texto libre no deje sin espacio a las demás.
    /// </summary>
    private static double[] ColumnWidthsCm(PdfTable data, TextMeasure measure)
    {
        var columns = data.Headers.Count;
        if (data.ColumnWidths is { } weights && weights.Count == columns && weights.All(w => w > 0))
        {
            var sum = weights.Sum();
            return weights.Select(w => ContentWidthCm * w / sum).ToArray();
        }

        var minimum = new double[columns];
        var natural = new double[columns];
        for (var i = 0; i < columns; i++)
        {
            var cells = data.Rows.Select(r => i < r.Count ? r[i] ?? string.Empty : string.Empty).ToList();
            var headerWord = Words(data.Headers[i]).Select(w => measure.WidthCm(w, TableFontSize, bold: true)).DefaultIfEmpty(0).Max();
            var longestWord = cells
                .SelectMany(Lines)
                .SelectMany(line => Words(line.Text).Select(w => measure.WidthCm(w, line.Size, bold: false)))
                .DefaultIfEmpty(0)
                .Max();
            var longestLine = cells
                .SelectMany(Lines)
                .Select(line => measure.WidthCm(line.Text, line.Size, bold: false))
                .Append(measure.WidthCm(data.Headers[i], TableFontSize, bold: true))
                .Max();

            minimum[i] = Math.Max(Math.Max(headerWord, longestWord), 0.6) + CellPaddingCm;
            natural[i] = Math.Min(longestLine, 7.5) + CellPaddingCm;
        }

        var minimumSum = minimum.Sum();
        if (minimumSum >= ContentWidthCm)
            return minimum.Select(m => ContentWidthCm * m / minimumSum).ToArray();

        var naturalSum = natural.Sum();
        if (naturalSum <= ContentWidthCm)
        {
            // Todo cabe en una línea: el espacio sobrante se reparte en proporción al ancho natural.
            return natural.Select(n => ContentWidthCm * n / naturalSum).ToArray();
        }

        // Se reparte el espacio sobre los mínimos en proporción a lo que a cada columna le falta para su ancho natural.
        var extra = ContentWidthCm - minimumSum;
        var wanted = natural.Select((n, i) => Math.Max(0, n - minimum[i])).ToArray();
        var wantedSum = wanted.Sum();
        return minimum.Select((m, i) => m + (wantedSum <= 0 ? extra / columns : extra * wanted[i] / wantedSum)).ToArray();
    }

    private static IEnumerable<(string Text, double Size)> Lines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select((line, index) => (line, index == 0 ? TableFontSize : SecondaryFontSize));

    private static IEnumerable<string> Words(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Agrega el texto al párrafo; una palabra más ancha que la columna (solo si las mínimas no caben) se corta con
    /// saltos de línea para que nunca invada la columna vecina.
    /// </summary>
    private static void AddWrapped(Paragraph paragraph, string text, double columnCm, double size, bool bold, TextMeasure measure)
    {
        var available = columnCm - CellPaddingCm;
        var words = text.Split(' ');
        for (var w = 0; w < words.Length; w++)
        {
            if (w > 0)
                paragraph.AddText(" ");

            var word = words[w];
            if (word.Length == 0 || measure.WidthCm(word, size, bold) <= available)
            {
                paragraph.AddText(word);
                continue;
            }

            var chunk = string.Empty;
            foreach (var ch in word)
            {
                if (chunk.Length > 0 && measure.WidthCm(chunk + ch, size, bold) > available)
                {
                    paragraph.AddText(chunk);
                    paragraph.AddLineBreak();
                    chunk = string.Empty;
                }

                chunk += ch;
            }

            paragraph.AddText(chunk);
        }
    }

    private static void AddMultiline(Cell cell, string text, double size, bool bold)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var l = 0; l < lines.Length; l++)
        {
            var paragraph = cell.AddParagraph(lines[l]);
            paragraph.Format.Font.Size = l == 0 ? size : SecondaryFontSize + 0.5;
            paragraph.Format.Font.Bold = l == 0 && bold;
            if (l > 0)
                paragraph.Format.Font.Color = Muted;
        }
    }

    /// <summary>Columnas sin índice explícito cuyos valores son todos montos (con separador decimal o de miles).</summary>
    private static IEnumerable<int> DetectNumeric(PdfTable data)
    {
        for (var i = 0; i < data.Headers.Count; i++)
        {
            var values = data.Rows
                .Select(r => i < r.Count ? r[i]?.Trim() ?? string.Empty : string.Empty)
                .Where(v => v.Length > 0 && v != "-")
                .ToList();
            if (values.Count > 0 && values.All(v => AmountPattern().IsMatch(v)) && values.Any(v => v.Contains(',') || v.Contains('.')))
                yield return i;
        }
    }

    [GeneratedRegex(@"^-?\d{1,3}(\.\d{3})*(,\d+)?$")]
    private static partial Regex AmountPattern();

    private static (Color Fore, Color Back) ToneColors(string? tone) => tone switch
    {
        PdfTones.Success => (new Color(21, 115, 59), new Color(230, 244, 235)),
        PdfTones.Warning => (new Color(166, 77, 0), new Color(255, 242, 224)),
        PdfTones.Danger => (new Color(176, 32, 32), new Color(252, 233, 233)),
        PdfTones.Info => (Navy, new Color(229, 237, 245)),
        _ => (new Color(80, 88, 98), LabelShade)
    };

    /// <summary>Mide texto con la fuente incrustada (contexto de medición de PDFsharp), en cm.</summary>
    private sealed class TextMeasure : IDisposable
    {
        private readonly XGraphics _graphics;
        private readonly Dictionary<(double, bool), XFont> _fonts = [];

        private TextMeasure(XGraphics graphics) => _graphics = graphics;

        public static TextMeasure Create() =>
            new(XGraphics.CreateMeasureContext(new XSize(2000, 2000), XGraphicsUnit.Point, XPageDirection.Downwards));

        public double WidthCm(string text, double size, bool bold)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (!_fonts.TryGetValue((size, bold), out var font))
            {
                font = new XFont(EmbeddedFontResolver.FamilyName, size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
                _fonts[(size, bold)] = font;
            }

            return _graphics.MeasureString(text, font).Width / PointsPerCm;
        }

        public void Dispose() => _graphics.Dispose();
    }
}
