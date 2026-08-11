namespace HapagPortal.UnitTests.Application.Reports;

using FluentAssertions;
using HapagPortal.Application.Reports.Common;

public sealed class CsvExporterTests
{
    [Fact]
    public void ToCsv_WritesHeaderAndRows()
    {
        var report = new ReportResult("R", ["A", "B"],
            [["1", "2"], ["3", "4"]]);

        var csv = CsvExporter.ToCsv(report).Replace("\r\n", "\n").TrimEnd('\n');

        csv.Should().Be("A;B\n1;2\n3;4");
    }

    [Fact]
    public void ToCsv_EscapesDelimiterQuotesAndNewlines()
    {
        var report = new ReportResult("R", ["Col"],
            [["a;b"], ["say \"hi\""], ["line1\nline2"]]);

        var csv = CsvExporter.ToCsv(report).Replace("\r\n", "\n");

        csv.Should().Contain("\"a;b\"");
        csv.Should().Contain("\"say \"\"hi\"\"\"");
        csv.Should().Contain("\"line1\nline2\"");
    }
}
