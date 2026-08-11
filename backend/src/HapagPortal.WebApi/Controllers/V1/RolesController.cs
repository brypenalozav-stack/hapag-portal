namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Roles.GetRoles;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
public sealed class RolesController : ApiController
{
    [HttpGet]
    [HasPermission("users.manage")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetRolesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
