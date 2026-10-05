namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Listado y detalle únicos de embarques accesibles por el usuario (M2-06, M2-07). La autorización
/// por BL se resuelve en Application con el evaluador de accesos de M1-11.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/shipments")]
public sealed class ShipmentsController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] SearchShipmentsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{blNumber}")]
    public async Task<IActionResult> GetDetail(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShipmentDetailQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
