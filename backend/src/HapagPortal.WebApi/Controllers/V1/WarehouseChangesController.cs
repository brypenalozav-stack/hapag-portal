namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.WarehouseChanges.Bulk;
using HapagPortal.Application.WarehouseChanges.History;
using HapagPortal.Application.WarehouseChanges.Read.GetById;
using HapagPortal.Application.WarehouseChanges.Read.GetMyChanges;
using HapagPortal.Application.WarehouseChanges.Requests;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/warehouse-changes")]
/// <summary>
/// Cambio de almacén. El POST /warehouse-changes anterior, que aceptaba el monto enviado por el cliente,
/// se retiró en la Ola C: las solicitudes van por /requests (individual) y /bulk (masiva), donde el
/// monto lo determina el servidor según el derecho a cambio gratuito o la tarifa vigente (M3-04, M8-01).
/// </summary>
public sealed class WarehouseChangesController : ApiController
{
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

    /// <summary>
    /// Historial de cambios de almacén (M3-06): fecha, estado, embarque y razón social y RUT de quien pagó; filtrable
    /// por BL o booking para reconstruir la trazabilidad desde el embarque. Incluye los creados por solicitudes masivas.
    /// </summary>
    [HttpGet("history")]
    [RequiresFeature(FeatureNames.WarehouseHistory)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? blNumber,
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Sender.Send(
            new GetWarehouseChangeHistoryQuery(blNumber, status, from, to, organizationId, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Línea de tiempo de una solicitud: creación, derecho gratuito, estados del pago y liberación (M3-06).</summary>
    [HttpGet("history/{id:guid}")]
    [RequiresFeature(FeatureNames.WarehouseHistory)]
    public async Task<IActionResult> GetTrace(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetWarehouseChangeTraceQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
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

    /// <summary>Derecho a cambio gratuito y tarifas vigentes para un BL (M3-04, M8-01).</summary>
    [HttpGet("quote/{blNumber}")]
    public async Task<IActionResult> GetQuote(
        string blNumber,
        [FromQuery] string? containerNumber,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetWarehouseChangeQuoteQuery(blNumber, containerNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Solicitud individual: gratuita se completa sin cobro; si no, queda pendiente de pago (M3-04).</summary>
    [HttpPost("requests")]
    public async Task<IActionResult> RequestChange(
        [FromBody] RequestWarehouseChangeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Solicitud masiva (M3-05): se procesa en segundo plano; el avance se consulta por su ID (NF-19).</summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> SubmitBulk(
        [FromBody] SubmitWarehouseChangeBatchCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetBulk), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpGet("bulk")]
    public async Task<IActionResult> GetBulkRequests(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetWarehouseChangeBatchesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("bulk/{id:guid}")]
    public async Task<IActionResult> GetBulk(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetWarehouseChangeBatchQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
