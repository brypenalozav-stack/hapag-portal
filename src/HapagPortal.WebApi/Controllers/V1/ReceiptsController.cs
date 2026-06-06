namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Receipts.Commands.Create;
using HapagPortal.Application.Receipts.Read.GetById;
using HapagPortal.Application.Receipts.Read.GetMyReceipts;
using HapagPortal.Application.Receipts.Read.GetPdf;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
public sealed class ReceiptsController : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.PaymentId }, result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetReceiptByIdQuery(id);
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
        var query = new GetReceiptPdfQuery(id);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? File(result.Value, "application/pdf", $"receipt-{id}.pdf")
            : HandleFailure(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyReceipts(
        CancellationToken cancellationToken)
    {
        var query = new GetMyReceiptsQuery();
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }
}
