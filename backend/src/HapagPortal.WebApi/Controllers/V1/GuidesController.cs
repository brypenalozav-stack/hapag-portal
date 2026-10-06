namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Guides;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Modo guía (M1-27): guías activas para el usuario con sus pasos ES/EN y su estado (completada o descartada); el usuario
/// puede desactivarla en cualquier momento y volver a activarla (<c>Reset</c>).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/guides")]
public sealed class GuidesController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? route, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetGuidesQuery(route), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetGuideQuery(code), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{code}/state")]
    public async Task<IActionResult> SetState(string code, [FromBody] GuideStateRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SetGuideStateCommand(code, request.Status ?? string.Empty, request.LastStep), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record GuideStateRequest(string? Status, int? LastStep);
