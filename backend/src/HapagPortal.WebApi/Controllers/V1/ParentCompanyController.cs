namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Organizations.ParentCompany;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Visibilidad hacia la empresa matriz (M1-21): la filial pide asociarse a su matriz (Hapag-Lloyd lo aprueba) y decide si
/// la matriz ve sus BL; la matriz consulta aquí sus filiales. Cambiar requiere <c>org.access.manage</c>.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/organizations/me/parent-company")]
[RequiresFeature(FeatureNames.ParentCompany)]
public sealed class ParentCompanyController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetParentCompanyQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("candidates")]
    public async Task<IActionResult> Candidates([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetParentCandidatesQuery(search), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> RequestLink([FromBody] RequestParentLinkCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : HandleFailure(result);
    }

    [HttpPut("visibility")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> SetVisibility([FromBody] SetParentVisibilityCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> Remove(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveParentLinkCommand(), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
