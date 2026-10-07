namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Announcements;
using HapagPortal.Application.Config.Features;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Comunicados vigentes para el usuario, por país y operación, con su fecha de publicación (M1-26).</summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/announcements")]
[RequiresFeature(FeatureNames.Announcements)]
public sealed class AnnouncementsController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetCurrent([FromQuery] string? country, [FromQuery] string? operation, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCurrentAnnouncementsQuery(country, operation), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
