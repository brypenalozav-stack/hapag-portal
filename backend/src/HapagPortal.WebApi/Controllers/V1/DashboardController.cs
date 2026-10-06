namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Dashboard;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Dashboard consolidado del cliente (M1-05): gestiones en curso, servicios pendientes de pago, documentos
/// recientes e indicadores operativos, filtrados en el servidor por los accesos del usuario (M1-11, NF-05).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? operation,
        [FromQuery] string? country,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDashboardQuery(operation, country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
