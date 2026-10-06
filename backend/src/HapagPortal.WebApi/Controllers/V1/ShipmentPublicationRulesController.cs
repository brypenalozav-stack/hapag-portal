namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Shipments.Publication;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Mantenedor interno de las reglas de publicación por DIFU de destino final (M2-01), con registro de cambios
/// (NF-15). Los BL no publicados dejan de verse para los clientes en cuanto cambia la regla.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/shipment-publication-rules")]
public sealed class ShipmentPublicationRulesController : ApiController
{
    [HttpGet]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? country,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShipmentPublicationRulesQuery(country, includeInactive), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/history")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShipmentPublicationRuleHistoryQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] ShipmentPublicationRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateShipmentPublicationRuleCommand(
            request.Country, request.FinalDestinationCode, request.FinalDestinationName, request.DischargePortCode,
            request.Description), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ShipmentPublicationRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateShipmentPublicationRuleCommand(
            id, request.Country, request.FinalDestinationCode, request.FinalDestinationName, request.DischargePortCode,
            request.Description), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateShipmentPublicationRuleCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}

public sealed record ShipmentPublicationRuleRequest(
    string Country,
    string FinalDestinationCode,
    string? FinalDestinationName,
    string? DischargePortCode,
    string? Description);
