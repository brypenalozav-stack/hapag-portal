namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Payments.Maintainers;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Bloqueo programado de pagos por horario (M8-07): mantenedor interno con registro de cada alta,
/// modificación y cancelación (NF-15) y consulta del bloqueo vigente para el cliente.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/payment-blocks")]
public sealed class PaymentBlocksController : ApiController
{
    [HttpGet]
    [HasPermission(PaymentPermissions.ManageBlockWindows)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? country,
        [FromQuery] bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentBlockWindowsQuery(country, includeCancelled), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Bloqueo vigente en el país (por defecto, el del usuario). Disponible para cualquier usuario.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string? country, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentBlockStatusQuery(country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(PaymentPermissions.ManageBlockWindows)]
    public async Task<IActionResult> Create([FromBody] PaymentBlockWindowRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreatePaymentBlockWindowCommand(
            request.Country, request.StartDate, request.StartTime, request.EndDate, request.EndTime, request.Reason,
            request.ClientMessage), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PaymentPermissions.ManageBlockWindows)]
    public async Task<IActionResult> Update(Guid id, [FromBody] PaymentBlockWindowRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdatePaymentBlockWindowCommand(
            id, request.Country, request.StartDate, request.StartTime, request.EndDate, request.EndTime, request.Reason,
            request.ClientMessage), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(PaymentPermissions.ManageBlockWindows)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CancelPaymentBlockWindowCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("{id:guid}/history")]
    [HasPermission(PaymentPermissions.ManageBlockWindows)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentBlockWindowHistoryQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record PaymentBlockWindowRequest(
    string? Country,
    DateOnly StartDate,
    TimeOnly StartTime,
    DateOnly EndDate,
    TimeOnly EndTime,
    string Reason,
    string ClientMessage);
