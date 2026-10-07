namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Demurrage.Advance;
using HapagPortal.Application.Demurrage.Calculate;
using HapagPortal.Application.Demurrage.Read.GetByBL;
using HapagPortal.Application.Demurrage.Read.GetByContainer;
using HapagPortal.Application.Demurrage.State;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
public sealed class DemurrageController : ApiController
{
    [HttpGet("{blNumber}")]
    public async Task<IActionResult> GetByBL(
        string blNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetDemurrageByBLQuery(blNumber);
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
        var query = new GetDemurrageByContainerQuery(containerNumber);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    /// <summary>Estado del demurrage del BL y su acción (M3-18), con MHD (M3-02) y demoras anticipadas (M3-16).</summary>
    [HttpGet("{blNumber}/status")]
    public async Task<IActionResult> GetStatus(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDemurrageStatusQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Calculadora (M3-18): bloqueada si el BL ya tiene factura de demurrage.</summary>
    [HttpPost("{blNumber}/calculate")]
    public async Task<IActionResult> Calculate(
        string blNumber,
        [FromBody] CalculateDemurrageRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CalculateDemurrageCommand(
            blNumber,
            request?.UntilDate,
            request?.ContainerNumbers,
            request?.Save ?? true);

        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Genera el cargo de demoras anticipadas de Bolivia para el carro (M3-16).</summary>
    [HttpPost("{blNumber}/advance-demurrage")]
    public async Task<IActionResult> RequestAdvance(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RequestAdvanceDemurrageCommand(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record CalculateDemurrageRequest(
    DateOnly? UntilDate,
    IReadOnlyList<string>? ContainerNumbers,
    bool? Save);
