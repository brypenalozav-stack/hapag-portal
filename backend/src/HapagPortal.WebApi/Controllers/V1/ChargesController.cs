namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.ChargeRules.Charges;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Recargos de un BL con las reglas de Nexus aplicadas (Fase 1, Ola C): exenciones de Gate In, EDS y
/// Gate Out (M4-01, M4-02, M3-01), exclusión del IPO por crédito (M4-03), carta FFWW (M4-04) y tipo de
/// cambio (M5-05). La autorización por BL se resuelve en Application con el evaluador de M1-11.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/charges")]
public sealed class ChargesController : ApiController
{
    [HttpGet("{blNumber}")]
    public async Task<IActionResult> Get(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShipmentChargesQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Aplica las exenciones de Nexus; si todo queda exento, el proceso se completa sin carro (M4-02).</summary>
    [HttpPost("{blNumber}/apply-rules")]
    public async Task<IActionResult> ApplyRules(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ApplyChargeRulesCommand(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
