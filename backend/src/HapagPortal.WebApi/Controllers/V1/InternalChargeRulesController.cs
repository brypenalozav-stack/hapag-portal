namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.InternalChargeRules;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Reglas internas por cuenta (M3-04 cambio de almacén gratuito, M3-16 demoras anticipadas de Bolivia),
/// con registro de cambios (NF-15). Crédito, FFWW y exenciones se leen de Nexus y no se administran aquí.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/internal-charge-rules")]
public sealed class InternalChargeRulesController : ApiController
{
    [HttpGet]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ruleType,
        [FromQuery] string? country,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInternalChargeRulesQuery(ruleType, country, includeInactive), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/history")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInternalChargeRuleHistoryQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] InternalChargeRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateInternalChargeRuleCommand(
            request.RuleType, request.Country, request.TaxId, request.MatchCode, request.AccountName, request.Reason,
            request.MaxUsesPerBl, request.ValidFrom, request.ValidTo), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] InternalChargeRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateInternalChargeRuleCommand(
            id, request.RuleType, request.Country, request.TaxId, request.MatchCode, request.AccountName, request.Reason,
            request.MaxUsesPerBl, request.ValidFrom, request.ValidTo), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateInternalChargeRuleCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}

public sealed record InternalChargeRuleRequest(
    string RuleType,
    string Country,
    string? TaxId,
    string? MatchCode,
    string? AccountName,
    string? Reason,
    int? MaxUsesPerBl,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
