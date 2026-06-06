namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.LocalCharges.Read.GetByBL;
using HapagPortal.Application.LocalCharges.Read.GetByContainer;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/local-charges")]
public sealed class LocalChargesController : ApiController
{
    [HttpGet("{blNumber}")]
    public async Task<IActionResult> GetByBL(
        string blNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetLocalChargesByBLQuery(blNumber);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("container/{containerNumber}")]
    public async Task<IActionResult> GetByContainer(
        string containerNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetLocalChargesByContainerQuery(containerNumber);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }
}
