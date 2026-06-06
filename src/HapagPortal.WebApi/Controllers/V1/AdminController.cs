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

[ApiVersion("1.0")]
[Authorize(Roles = "Admin,BA")]
[Route("api/v{version:apiVersion}/admin")]
public sealed class AdminController : ApiController
{
    [HttpGet("credit-clients")]
    public async Task<IActionResult> GetCreditClients(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAllCreditClientsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("credit-clients")]
    public async Task<IActionResult> CreateCreditClient(
        [FromBody] CreateCreditClientCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("credit-clients/{id:guid}")]
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
    public async Task<IActionResult> GetDemurrageExemptions(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAllDemurrageExemptionsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("demurrage-exemptions")]
    public async Task<IActionResult> CreateDemurrageExemption(
        [FromBody] CreateDemurrageExemptionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("demurrage-exemptions/{id:guid}")]
    public async Task<IActionResult> DeactivateDemurrageExemption(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateDemurrageExemptionCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
