namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Admin.CreditClients.Commands.Create;
using HapagPortal.Application.Admin.CreditClients.Commands.Update;
using HapagPortal.Application.Admin.CreditClients.Read.GetAll;
using HapagPortal.Application.Admin.DemurrageExemptions.Commands.Create;
using HapagPortal.Application.Admin.DemurrageExemptions.Commands.Deactivate;
using HapagPortal.Application.Admin.DemurrageExemptions.Read.GetAll;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Mantenedores heredados de clientes con crédito y exenciones de demurrage. Desde la Ola C el portal
/// lee el crédito, las exenciones y los FFWW desde Nexus (M8-02, M8-03, M4-01) sin listado paralelo, y
/// la interfaz de estos mantenedores se retiró: los endpoints quedan marcados como obsoletos (las
/// tablas se conservan) hasta que se decida eliminarlos.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/admin")]
public sealed class AdminController : ApiController
{
    [HttpGet("credit-clients")]
    [Obsolete("Fase 1, Ola C (M8-02): la condición de crédito se lee de Nexus. Mantenedor heredado sin interfaz; no se usa en los cobros.")]
    public async Task<IActionResult> GetCreditClients(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAllCreditClientsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("credit-clients")]
    [Obsolete("Fase 1, Ola C (M8-02): la condición de crédito se lee de Nexus. Mantenedor heredado sin interfaz; no se usa en los cobros.")]
    public async Task<IActionResult> CreateCreditClient(
        [FromBody] CreateCreditClientCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("credit-clients/{id:guid}")]
    [Obsolete("Fase 1, Ola C (M8-02): la condición de crédito se lee de Nexus. Mantenedor heredado sin interfaz; no se usa en los cobros.")]
    public async Task<IActionResult> UpdateCreditClient(
        Guid id,
        [FromBody] UpdateCreditClientCommand command,
        CancellationToken cancellationToken)
    {
        var updateCommand = command with { Id = id };
        var result = await Sender.Send(updateCommand, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("demurrage-exemptions")]
    [Obsolete("Fase 1, Ola C (M8-02, M4-01): las exenciones se leen de Nexus. Mantenedor heredado sin interfaz; no se usa en los cobros.")]
    public async Task<IActionResult> GetDemurrageExemptions(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAllDemurrageExemptionsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("demurrage-exemptions")]
    [Obsolete("Fase 1, Ola C (M8-02, M4-01): las exenciones se leen de Nexus. Mantenedor heredado sin interfaz; no se usa en los cobros.")]
    public async Task<IActionResult> CreateDemurrageExemption(
        [FromBody] CreateDemurrageExemptionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("demurrage-exemptions/{id:guid}")]
    [Obsolete("Fase 1, Ola C (M8-02, M4-01): las exenciones se leen de Nexus. Mantenedor heredado sin interfaz; no se usa en los cobros.")]
    public async Task<IActionResult> DeactivateDemurrageExemption(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateDemurrageExemptionCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
