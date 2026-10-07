namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Impersonation;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Sesión de «Vista como cliente» en curso, con el token de impersonación (M8-08): la interfaz muestra el aviso permanente
/// de impersonación con estos datos y la termina con <c>end</c> (también al cerrar sesión).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/impersonation")]
public sealed class ImpersonationController : ApiController
{
    [HttpGet("current")]
    [RequiresFeature(FeatureNames.Impersonation)]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCurrentImpersonationQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("end")]
    public async Task<IActionResult> End(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new EndCurrentImpersonationCommand(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
