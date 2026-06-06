namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Payments.Commands.Cancel;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Application.Payments.Create;
using HapagPortal.Application.Payments.Read.GetById;
using HapagPortal.Application.Payments.Read.GetMyPayments;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
public sealed class PaymentsController : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetPaymentByIdQuery(id);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyPayments(
        CancellationToken cancellationToken)
    {
        var query = new GetMyPaymentsQuery();
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "Admin,BA")]
    public async Task<IActionResult> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmPaymentCommand(id);
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new CancelPaymentCommand(id);
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost("webhook/khipu")]
    [AllowAnonymous]
    public async Task<IActionResult> KhipuWebhook(
        [FromBody] KhipuWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : HandleFailure(result);
    }

    [HttpPost("webhook/banco-chile")]
    [AllowAnonymous]
    public async Task<IActionResult> BancoChileWebhook(
        [FromBody] BancoChileWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : HandleFailure(result);
    }
}
