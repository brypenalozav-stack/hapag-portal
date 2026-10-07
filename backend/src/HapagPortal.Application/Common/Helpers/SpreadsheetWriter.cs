namespace HapagPortal.Application.Common.Helpers;

using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

/// <summary>Hoja de una planilla: nombre y filas (la primera es el encabezado, en negrita).</summary>
public sealed record SpreadsheetSheet(string Name, IReadOnlyList<IReadOnlyList<object?>> Rows);

/// <summary>
/// Exportación a planilla sin dependencias externas: XLSX mínimo (Office Open XML con <c>System.IO.Compression</c>,
/// texto en línea, números nativos y encabezado en negrita) y CSV RFC 4180 en UTF-8 con BOM (separador coma,
/// números con punto decimal). Fechas como texto ISO (<c>yyyy-MM-dd</c>, <c>yyyy-MM-dd HH:mm</c>).
/// </summary>
public static class SpreadsheetWriter
{
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string CsvContentType = "text/csv; charset=utf-8";

    private const int MaxSheetNameLength = 31;

    public static byte[] ToXlsx(IReadOnlyList<SpreadsheetSheet> sheets)
    {
        if (sheets.Count == 0)
            throw new ArgumentException("At least one sheet is required.", nameof(sheets));

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
            Write(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            Write(zip, "xl/workbook.xml", Workbook(sheets));
            Write(zip, "xl/_rels/workbook.xml.rels", WorkbookRelationships(sheets.Count));
            Write(zip, "xl/styles.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
                "<xf numFmtId=\"4\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/></cellXfs>" +
                "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                "</styleSheet>");

            for (var i = 0; i < sheets.Count; i++)
                Write(zip, $"xl/worksheets/sheet{i + 1}.xml", Worksheet(sheets[i]));
        }

        return buffer.ToArray();
    }

    public static byte[] ToCsv(IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var text = new StringBuilder();
        foreach (var row in rows)
        {
            text.Append(string.Join(',', row.Select(cell => Quote(Format(cell)))));
            text.Append("\r\n");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }

    /// <summary>Referencia de columna de Excel (0 → A, 26 → AA).</summary>
    public static string ColumnName(int index)
    {
        var name = string.Empty;
        for (var n = index + 1; n > 0; n = (n - 1) / 26)
            name = (char)('A' + ((n - 1) % 26)) + name;
        return name;
    }

    private static string Format(object? cell) => cell switch
    {
        null => string.Empty,
        decimal d => d.ToString(CultureInfo.InvariantCulture),
        int i => i.ToString(CultureInfo.InvariantCulture),
        long l => l.ToString(CultureInfo.InvariantCulture),
        double f => f.ToString(CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        _ => Convert.ToString(cell, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static string Quote(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static void Write(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var stream = entry.Open();
        var bytes = new UTF8Encoding(false).GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string ContentTypes(int sheets)
    {
        var text = new StringBuilder(
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        for (var i = 1; i <= sheets; i++)
            text.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        text.Append("</Types>");
        return text.ToString();
    }

    private static string Workbook(IReadOnlyList<SpreadsheetSheet> sheets)
    {
        var text = new StringBuilder(
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < sheets.Count; i++)
        {
            var name = SheetName(sheets[i].Name, i, used);
            text.Append($"<sheet name=\"{Escape(name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        }

        text.Append("</sheets></workbook>");
        return text.ToString();
    }

    private static string WorkbookRelationships(int sheets)
    {
        var text = new StringBuilder(
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (var i = 1; i <= sheets; i++)
            text.Append($"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
        text.Append($"<Relationship Id=\"rId{sheets + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        text.Append("</Relationships>");
        return text.ToString();
    }

    private static string Worksheet(SpreadsheetSheet sheet)
    {
        var text = new StringBuilder(
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            text.Append($"<row r=\"{r + 1}\">");
            var row = sheet.Rows[r];
            for (var c = 0; c < row.Count; c++)
            {
                var reference = $"{ColumnName(c)}{r + 1}";
                var header = r == 0 ? " s=\"1\"" : string.Empty;
                switch (row[c])
                {
                    case null:
                        break;
                    case decimal or int or long or double when r > 0:
                        text.Append($"<c r=\"{reference}\" s=\"2\"><v>{Format(row[c])}</v></c>");
                        break;
                    default:
                        text.Append($"<c r=\"{reference}\" t=\"inlineStr\"{header}><is><t xml:space=\"preserve\">{Escape(Format(row[c]))}</t></is></c>");
                        break;
                }
            }

            text.Append("</row>");
        }

        text.Append("</sheetData></worksheet>");
        return text.ToString();
    }

    private static string SheetName(string name, int index, HashSet<string> used)
    {
        var clean = new string((name ?? string.Empty).Where(ch => ch is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\')).ToArray()).Trim();
        if (clean.Length == 0)
            clean = $"Hoja{index + 1}";
        if (clean.Length > MaxSheetNameLength)
            clean = clean[..MaxSheetNameLength];
        while (!used.Add(clean))
            clean = $"{clean[..Math.Min(clean.Length, MaxSheetNameLength - 3)]}_{index + 1}";
        return clean;
    }

    /// <summary>Escapa para XML y quita los caracteres de control que XML 1.0 no admite.</summary>
    private static string Escape(string value) =>
        SecurityElement.Escape(new string(value.Where(ch => ch is '\t' or '\n' or '\r' || ch >= ' ').ToArray())) ?? string.Empty;
}
