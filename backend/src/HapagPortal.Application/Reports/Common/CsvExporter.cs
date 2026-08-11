namespace HapagPortal.Application.Reports.Common;

using System.Text;

/// <summary>
/// Exporta un <see cref="ReportResult"/> a CSV (RFC 4180). Puro y sin dependencias externas:
/// entrecomilla los campos con separador, comillas o saltos de línea y duplica las comillas.
/// </summary>
public static class CsvExporter
{
    public static string ToCsv(ReportResult report, char delimiter = ';')
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(delimiter, report.Columns.Select(c => Escape(c, delimiter))));
        foreach (var row in report.Rows)
            sb.AppendLine(string.Join(delimiter, row.Select(v => Escape(v, delimiter))));
        return sb.ToString();
    }

    private static string Escape(string? value, char delimiter)
    {
        var v = value ?? string.Empty;
        var mustQuote = v.Contains(delimiter) || v.Contains('"') || v.Contains('\n') || v.Contains('\r');
        if (mustQuote)
            v = "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }
}
