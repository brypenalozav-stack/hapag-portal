namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.BillsOfLading.Read.GetAll;
using HapagPortal.Application.BillsOfLading.Read.GetByNumber;
using HapagPortal.Application.BillsOfLading.Read.GetCharges;
using HapagPortal.Application.BillsOfLading.Read.GetContainers;
using HapagPortal.Application.BillsOfLading.Read.GetMyBLs;
using HapagPortal.Application.Demurrage.Read.GetByBL;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/bills-of-lading")]
public sealed class BillsOfLadingController : ApiController
{
    [HttpGet("{blNumber}")]
    public async Task<IActionResult> GetByNumber(
        string blNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetBLByNumberQuery(blNumber);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? country,
        [FromQuery] Guid? clientId,
        CancellationToken cancellationToken)
    {
        var query = new GetAllBLsQuery(country, clientId);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{blNumber}/containers")]
    public async Task<IActionResult> GetContainers(
        string blNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetContainersByBLQuery(blNumber);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{blNumber}/charges")]
    public async Task<IActionResult> GetCharges(
        string blNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetChargesByBLQuery(blNumber);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{blNumber}/demurrage")]
    public async Task<IActionResult> GetDemurrage(
        string blNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetDemurrageByBLQuery(blNumber);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBLs(
        CancellationToken cancellationToken)
    {
        var query = new GetMyBLsQuery();
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }
}
