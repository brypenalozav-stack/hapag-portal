namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Issuance;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Application.Shipments.Tatc;
using HapagPortal.Application.ThirdPartyAccess.OpenAccess;
using HapagPortal.Application.ThirdPartyAccess.Widenings;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Listado y detalle únicos de embarques accesibles por el usuario (M2-06, M2-07), autoasociación por
/// acceso abierto (M1-18) y ampliación de visibilidad entre roles (M1-16), estado de emisión del BL (M2-02),
/// consulta del TATC y su generación masiva (M2-09). La autorización por BL se resuelve en Application con el
/// evaluador de accesos de M1-11, que además excluye los BL no publicados por DIFU (M2-01).
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

    /// <summary>Estado de emisión del documento de transporte leído del origen (M2-02).</summary>
    [HttpGet("{blNumber}/issuance")]
    public async Task<IActionResult> GetIssuance(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShipmentIssuanceQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Estado del BL y de su TATC por contenedor (M2-09).</summary>
    [HttpGet("{blNumber}/tatc")]
    public async Task<IActionResult> GetTatc(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShipmentTatcQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Generación masiva de TATC para BL de una misma localidad (M2-09).</summary>
    [HttpPost("tatc-batches")]
    public async Task<IActionResult> RequestTatcBatch(
        [FromBody] RequestTatcBatchCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("tatc-batches")]
    public async Task<IActionResult> GetTatcBatches(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTatcBatchesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("tatc-batches/{id:guid}")]
    public async Task<IActionResult> GetTatcBatch(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTatcBatchQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Autoasociación a un BL consultado con acceso abierto (M1-18); requiere un perfil que opere.</summary>
    [HttpPost("{blNumber}/associate")]
    public async Task<IActionResult> Associate(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SelfAssociateShipmentCommand(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Ampliaciones de visibilidad del BL hacia otros roles (M1-16).</summary>
    [HttpGet("{blNumber}/visibility-widenings")]
    public async Task<IActionResult> GetWidenings(
        string blNumber,
        [FromQuery] bool includeRevoked,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetVisibilityWideningsQuery(blNumber, includeRevoked), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{blNumber}/visibility-widenings")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> CreateWidenings(
        string blNumber,
        [FromBody] CreateVisibilityWideningsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { BlNumber = blNumber }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{blNumber}/visibility-widenings/{id:guid}/revoke")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> RevokeWidening(string blNumber, Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeVisibilityWideningCommand(blNumber, id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
