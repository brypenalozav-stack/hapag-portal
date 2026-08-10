namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Users.Create;
using HapagPortal.Application.Users.Search;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
public sealed class UsersController : ApiController
{
    [HttpGet]
    [HasPermission("users.manage")]
    public async Task<IActionResult> Search(
        [FromQuery] SearchUsersQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission("users.manage")]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Create), result.Value)
            : HandleFailure(result);
    }
}
