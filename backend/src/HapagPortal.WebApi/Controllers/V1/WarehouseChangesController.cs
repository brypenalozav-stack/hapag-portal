namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.WarehouseChanges.Commands.Create;
using HapagPortal.Application.WarehouseChanges.Read.GetById;
using HapagPortal.Application.WarehouseChanges.Read.GetMyChanges;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/warehouse-changes")]
public sealed class WarehouseChangesController : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWarehouseChangeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyWarehouseChanges(
        CancellationToken cancellationToken)
    {
        var query = new GetMyWarehouseChangesQuery();
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetWarehouseChangeByIdQuery(id);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }
}
