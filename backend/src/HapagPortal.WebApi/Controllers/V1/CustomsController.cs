namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Customs.Amend;
using HapagPortal.Application.Customs.Manifests;
using HapagPortal.Application.Customs.Read;
using HapagPortal.Application.Customs.Transmit;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/customs")]
public sealed class CustomsController : ApiController
{
    [HttpGet("manifests")]
    [HasPermission("customs.view")]
    public async Task<IActionResult> GetManifests([FromQuery] string? direction, CancellationToken ct)
    {
        var result = await Sender.Send(new GetManifestsQuery(direction), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("manifests")]
    [HasPermission("customs.transmit")]
    public async Task<IActionResult> CreateManifest([FromBody] CreateManifestCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return result.IsSuccess ? Ok(new { id = result.Value }) : HandleFailure(result);
    }

    [HttpGet("transmissions")]
    [HasPermission("customs.view")]
    public async Task<IActionResult> GetTransmissions(
        [FromQuery] Guid? manifestId,
        [FromQuery] string? stage,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var result = await Sender.Send(new GetTransmissionsQuery(manifestId, stage, status), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("manifests/{manifestId:guid}/transmit-header")]
    [HasPermission("customs.transmit")]
    public async Task<IActionResult> TransmitHeader(Guid manifestId, CancellationToken ct)
    {
        var result = await Sender.Send(new TransmitManifestHeaderCommand(manifestId), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("manifests/{manifestId:guid}/transmit-bl/{blId:guid}")]
    [HasPermission("customs.transmit")]
    public async Task<IActionResult> TransmitBL(Guid manifestId, Guid blId, CancellationToken ct)
    {
        var result = await Sender.Send(new TransmitBLCommand(manifestId, blId), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("transmissions/{transmissionId:guid}/retry")]
    [HasPermission("customs.retry")]
    public async Task<IActionResult> Retry(Guid transmissionId, CancellationToken ct)
    {
        var result = await Sender.Send(new RetryTransmissionCommand(transmissionId), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("manifests/{manifestId:guid}/amend")]
    [HasPermission("customs.transmit")]
    public async Task<IActionResult> Amend(
        Guid manifestId,
        [FromBody] AmendRequest body,
        CancellationToken ct)
    {
        var result = await Sender.Send(
            new SubmitManifestAmendmentCommand(manifestId, body.BillOfLadingId, body.Reason), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    public sealed record AmendRequest(Guid? BillOfLadingId, string Reason);
}
