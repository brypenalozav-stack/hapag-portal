namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Application.Tariffs.Maintainer;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Mantenedor de tarifas de cargos locales (M8-01) del portal interno, con registro de cambios (NF-15),
/// tarifas vigentes con la precedencia portal → Nexus y catálogo de conceptos de cobro.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/tariffs")]
public sealed class TariffsController : ApiController
{
    [HttpGet]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? country,
        [FromQuery] string? concept,
        [FromQuery] DateOnly? inForceOn,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTariffsQuery(country, concept, inForceOn, includeInactive), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTariffQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/history")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTariffHistoryQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] TariffRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateTariffCommand(
            request.ConceptCode, request.Code, request.Country, request.Currency, request.ContainerType, request.Description,
            request.Amount, request.TierUnit, request.TierMode, request.Tiers, request.ValidFrom, request.ValidTo), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TariffRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateTariffCommand(
            id, request.ConceptCode, request.Code, request.Country, request.Currency, request.ContainerType, request.Description,
            request.Amount, request.TierUnit, request.TierMode, request.Tiers, request.ValidFrom, request.ValidTo), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateTariffCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>
    /// Tarifas vigentes de un concepto (portal → Nexus), con el valor del tramo para una medida o un hito.
    /// Disponible para cualquier usuario autenticado: es el valor que el portal aplica al cobro.
    /// </summary>
    [HttpGet("in-force")]
    public async Task<IActionResult> GetInForce([FromQuery] GetTariffsInForceQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("concepts")]
    public async Task<IActionResult> GetConcepts([FromQuery] string? country, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetChargeConceptsQuery(country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record TariffRequest(
    string ConceptCode,
    string? Code,
    string Country,
    string Currency,
    string? ContainerType,
    string? Description,
    decimal Amount,
    string? TierUnit,
    string? TierMode,
    IReadOnlyList<TariffTierDto>? Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
