namespace HapagPortal.Application.Reports.Common;

/// <summary>Resultado tabular genérico de un reporte: columnas + filas (para render y export).</summary>
public sealed record ReportResult(
    string Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows);
