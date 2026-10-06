namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Admin.Overview;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Resumen del área de administración unificada (M8-05): secciones disponibles según los permisos y sus pendientes.</summary>
[ApiVersion("1.0")]
[Authorize]
[HasPermission(AdministrationPermissions.AccessAdminArea)]
[Route("api/v{version:apiVersion}/admin/overview")]
public sealed class AdminOverviewController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminOverviewQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
