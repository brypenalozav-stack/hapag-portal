namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.ServiceRequests.Definitions;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Mantenedor de definiciones de servicios on demand (M2-03, M2-04): reglas, formulario, tarifa, flujo y acción
/// de la matriz de cada concepto, con registro de cambios (NF-15). Habilitar un concepto nuevo es configurar una
/// definición (y su tarifa en el mantenedor de tarifas), sin desarrollo propio.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/service-definitions")]
[RequiresFeature(FeatureNames.OnDemandServices)]
public sealed class ServiceDefinitionsController : ApiController
{
    [HttpGet]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive,
        [FromQuery] string? country,
        [FromQuery] string? operation,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetServiceDefinitionsQuery(includeInactive, country, operation), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetServiceDefinitionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/history")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetServiceDefinitionHistoryQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] ServiceDefinitionInput request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateServiceDefinitionCommand(request), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServiceDefinitionInput request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateServiceDefinitionCommand(id, request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateServiceDefinitionCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
