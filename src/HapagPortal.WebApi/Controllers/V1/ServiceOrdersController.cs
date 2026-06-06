namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.ServiceOrders.Commands.Create;
using HapagPortal.Application.ServiceOrders.Read.GetMyOrders;
using HapagPortal.Application.ServiceOrders.Read.GetPdf;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/service-orders")]
public sealed class ServiceOrdersController : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Create), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyServiceOrders(
        CancellationToken cancellationToken)
    {
        var query = new GetMyServiceOrdersQuery();
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetServiceOrderPdfQuery(id);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? File(result.Value, "application/pdf", $"service-order-{id}.pdf")
            : HandleFailure(result);
    }
}
