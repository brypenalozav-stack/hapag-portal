namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Deadlines.Dashboard;
using HapagPortal.Application.Deadlines.GetRules;
using HapagPortal.Application.Deadlines.Recalculate;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/deadlines")]
public sealed class DeadlinesController : ApiController
{
    [HttpGet("rules")]
    [HasPermission("deadlines.view")]
    public async Task<IActionResult> GetRules(CancellationToken ct)
    {
        var result = await Sender.Send(new GetDeadlineRulesQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet]
    [HasPermission("deadlines.view")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? status,
        [FromQuery] Guid? manifestId,
        CancellationToken ct)
    {
        var result = await Sender.Send(new GetDeadlinesDashboardQuery(status, manifestId), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("manifests/{manifestId:guid}/recalculate")]
    [HasPermission("deadlines.view")]
    public async Task<IActionResult> Recalculate(Guid manifestId, CancellationToken ct)
    {
        var result = await Sender.Send(new RecalculateManifestDeadlinesCommand(manifestId), ct);
        return result.IsSuccess ? Ok(new { affected = result.Value }) : HandleFailure(result);
    }
}
