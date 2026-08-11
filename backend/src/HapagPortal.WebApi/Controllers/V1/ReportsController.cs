namespace HapagPortal.WebApi.Controllers.V1;

using System.Text;
using Asp.Versioning;
using HapagPortal.Application.Reports.Common;
using HapagPortal.Application.Reports.Deadlines;
using HapagPortal.Application.Reports.Transmissions;
using HapagPortal.Domain.Results;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController : ApiController
{
    [HttpGet("transmissions")]
    [HasPermission("reports.view")]
    public Task<IActionResult> Transmissions([FromQuery] string? status, CancellationToken ct)
        => Render(new GetTransmissionReportQuery(status), false, "transmisiones", ct);

    [HttpGet("transmissions/export")]
    [HasPermission("reports.view")]
    public Task<IActionResult> TransmissionsExport([FromQuery] string? status, CancellationToken ct)
        => Render(new GetTransmissionReportQuery(status), true, "transmisiones", ct);

    [HttpGet("overdue-deadlines")]
    [HasPermission("reports.view")]
    public Task<IActionResult> OverdueDeadlines([FromQuery] bool includeAtRisk, CancellationToken ct)
        => Render(new GetOverdueDeadlinesReportQuery(includeAtRisk), false, "plazos", ct);

    [HttpGet("overdue-deadlines/export")]
    [HasPermission("reports.view")]
    public Task<IActionResult> OverdueDeadlinesExport([FromQuery] bool includeAtRisk, CancellationToken ct)
        => Render(new GetOverdueDeadlinesReportQuery(includeAtRisk), true, "plazos", ct);

    private async Task<IActionResult> Render(
        MediatR.IRequest<Result<ReportResult>> query,
        bool export,
        string fileHint,
        CancellationToken ct)
    {
        var result = await Sender.Send(query, ct);
        if (!result.IsSuccess)
            return HandleFailure(result);

        if (!export)
            return Ok(result.Value);

        // BOM UTF-8 para que Excel abra los acentos correctamente.
        var csv = CsvExporter.ToCsv(result.Value);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"reporte-{fileHint}.csv");
    }
}
