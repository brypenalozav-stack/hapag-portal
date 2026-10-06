namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.AccessMatrix;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Administración de la matriz base de accesos por rol (M1-11) desde el portal interno.</summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/access-matrix")]
public sealed class AccessMatrixController : ApiController
{
    [HttpGet]
    [HasPermission(AccessPermissions.ManageAccessMatrix)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAccessMatrixQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{actionCode}")]
    [HasPermission(AccessPermissions.ManageAccessMatrix)]
    public async Task<IActionResult> UpdateLevel(
        string actionCode,
        [FromBody] UpdateAccessMatrixLevelCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { ActionCode = actionCode }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
