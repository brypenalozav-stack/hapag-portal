using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace HapagPortal.Infrastructure.Documents;

/// <summary>
/// Plantilla común de los documentos del portal con PDFsharp/MigraDoc 6 (MIT): encabezado con el emisor
/// (Hapag-Lloyd), título, número y fecha de emisión en el huso del país (NF-22), referencias del embarque,
/// secciones (campos, tablas, párrafos), nota de firma, código de verificación y numeración de páginas. La
/// fuente es Liberation Sans incrustada (<see cref="EmbeddedFontResolver"/>), igual en Windows y Linux.
/// </summary>
public sealed class MigraDocPdfRenderer : IPdfDocumentRenderer
{
    private const double ContentWidthCm = 17;

    private static readonly Lock FontGate = new();
    private static readonly EmbeddedFontResolver Fonts = new();

    private static readonly Color Navy = new(0, 43, 73);
    private static readonly Color Orange = new(245, 90, 0);
    private static readonly Color HeaderShade = new(230, 235, 240);
    private static readonly Color Muted = new(90, 90, 90);

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
        section.PageSetup.TopMargin = Unit.FromCentimeter(2.2);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(2.2);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2);

        AddHeader(section, model);
        AddFooter(section, model);
        AddTitle(section, model);
        AddFieldGrid(section, model.References);

        foreach (var part in model.Sections)
            AddSection(section, part);

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

        var heading = document.Styles.AddStyle("SectionHeading", StyleNames.Normal);
        heading.Font.Size = 10.5;
        heading.Font.Bold = true;
        heading.Font.Color = Navy;
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(12);
        heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);
        heading.ParagraphFormat.KeepWithNext = true;
        heading.ParagraphFormat.Borders.Bottom.Width = Unit.FromPoint(0.75);
        heading.ParagraphFormat.Borders.Bottom.Color = Orange;
    }

    private static void AddHeader(Section section, PdfDocumentModel model)
    {
        var header = section.Headers.Primary;

        var issuer = header.AddParagraph();
        issuer.AddFormattedText(model.Issuer, TextFormat.Bold);
        issuer.Format.Font.Size = 12;
        issuer.Format.Font.Color = Navy;

        var detail = header.AddParagraph(model.IssuerDetail);
        detail.Format.Font.Size = 8;
        detail.Format.Font.Color = Muted;
        detail.Format.Borders.Bottom.Width = Unit.FromPoint(1.5);
        detail.Format.Borders.Bottom.Color = Orange;
        detail.Format.SpaceAfter = Unit.FromPoint(6);
    }

    private static void AddFooter(Section section, PdfDocumentModel model)
    {
        var footer = section.Footers.Primary;

        if (!string.IsNullOrWhiteSpace(model.VerificationCode))
        {
            var verification = footer.AddParagraph();
            verification.AddText("Código de verificación: ");
            verification.AddFormattedText(model.VerificationCode, TextFormat.Bold);
            verification.Format.Font.Size = 8;
        }

        if (!string.IsNullOrWhiteSpace(model.Footer))
        {
            var text = footer.AddParagraph(model.Footer);
            text.Format.Font.Size = 7;
            text.Format.Font.Color = Muted;
        }

        var pages = footer.AddParagraph();
        pages.Format.Alignment = ParagraphAlignment.Right;
        pages.Format.Font.Size = 7;
        pages.AddText($"{model.DocumentNumber} - Página ");
        pages.AddPageField();
        pages.AddText(" de ");
        pages.AddNumPagesField();
    }

    private static void AddTitle(Section section, PdfDocumentModel model)
    {
        var title = section.AddParagraph();
        title.AddFormattedText(model.Title, TextFormat.Bold);
        title.Format.Font.Size = 15;
        title.Format.Font.Color = Navy;
        title.Format.SpaceAfter = Unit.FromPoint(2);

        if (!string.IsNullOrWhiteSpace(model.Subtitle))
        {
            var subtitle = section.AddParagraph(model.Subtitle);
            subtitle.Format.Font.Size = 10;
            subtitle.Format.Font.Color = Muted;
        }

        var meta = section.AddParagraph();
        meta.Format.SpaceBefore = Unit.FromPoint(6);
        meta.Format.SpaceAfter = Unit.FromPoint(8);
        meta.AddText("N° ");
        meta.AddFormattedText(model.DocumentNumber, TextFormat.Bold);
        meta.AddText($"   Emitido: {IssuedAt(model)}");
    }

    private static string IssuedAt(PdfDocumentModel model)
    {
        var country = model.TimeZoneId == BusinessCalendar.BoliviaTimeZone ? CountryCodes.Bolivia : CountryCodes.Chile;
        return $"{BusinessCalendar.ToLocal(country, model.IssuedAt):dd-MM-yyyy HH:mm} ({model.TimeZoneId})";
    }

    private static void AddSection(Section section, PdfSection part)
    {
        section.AddParagraph(part.Heading, "SectionHeading");

        if (part.Fields is { Count: > 0 } fields)
            AddFieldList(section, fields);

        if (part.Table is { } table)
        {
            if (part.Fields is { Count: > 0 })
                section.AddParagraph().Format.SpaceBefore = Unit.FromPoint(4);
            AddTable(section, table);
        }

        foreach (var text in part.Paragraphs ?? [])
        {
            var paragraph = section.AddParagraph(text);
            paragraph.Format.SpaceAfter = Unit.FromPoint(4);
            paragraph.Format.Alignment = ParagraphAlignment.Justify;
        }
    }

    /// <summary>Referencias del embarque en dos pares etiqueta/valor por fila.</summary>
    private static void AddFieldGrid(Section section, IReadOnlyList<PdfField> fields)
    {
        if (fields.Count == 0)
            return;

        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.5);
        table.Borders.Color = HeaderShade;
        foreach (var width in new[] { 2.6, 5.9, 2.6, 5.9 })
            table.AddColumn(Unit.FromCentimeter(width));

        for (var i = 0; i < fields.Count; i += 2)
        {
            var row = table.AddRow();
            FillField(row, 0, fields[i]);
            if (i + 1 < fields.Count)
                FillField(row, 2, fields[i + 1]);
        }
    }

    private static void AddFieldList(Section section, IReadOnlyList<PdfField> fields)
    {
        var table = section.AddTable();
        table.AddColumn(Unit.FromCentimeter(5));
        table.AddColumn(Unit.FromCentimeter(ContentWidthCm - 5));

        foreach (var field in fields)
            FillField(table.AddRow(), 0, field);
    }

    private static void FillField(Row row, int index, PdfField field)
    {
        row.Cells[index].Shading.Color = HeaderShade;
        row.Cells[index].AddParagraph().AddFormattedText(field.Label, TextFormat.Bold);
        row.Cells[index + 1].AddParagraph(string.IsNullOrWhiteSpace(field.Value) ? "-" : field.Value);
    }

    private static void AddTable(Section section, PdfTable data)
    {
        if (data.Headers.Count == 0)
            return;

        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.5);
        table.Borders.Color = HeaderShade;
        table.Format.Font.Size = 8;

        var width = Unit.FromCentimeter(ContentWidthCm / data.Headers.Count);
        foreach (var _ in data.Headers)
            table.AddColumn(width);

        var numeric = data.NumericColumns ?? [];

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Navy;
        for (var i = 0; i < data.Headers.Count; i++)
        {
            var paragraph = header.Cells[i].AddParagraph();
            paragraph.AddFormattedText(data.Headers[i], TextFormat.Bold);
            paragraph.Format.Font.Color = Colors.White;
            if (numeric.Contains(i))
                paragraph.Format.Alignment = ParagraphAlignment.Right;
        }

        foreach (var values in data.Rows)
        {
            var row = table.AddRow();
            for (var i = 0; i < data.Headers.Count; i++)
            {
                var paragraph = row.Cells[i].AddParagraph(i < values.Count ? values[i] : string.Empty);
                if (numeric.Contains(i))
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
            }
        }
    }
}
