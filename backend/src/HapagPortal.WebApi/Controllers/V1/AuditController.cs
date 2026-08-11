namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Audit.Search;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/audit")]
public sealed class AuditController : ApiController
{
    [HttpGet]
    [HasPermission("audit.view")]
    public async Task<IActionResult> Search(
        [FromQuery] SearchAuditQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
