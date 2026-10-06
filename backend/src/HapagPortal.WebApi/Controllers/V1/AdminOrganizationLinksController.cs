namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Organizations.ParentCompany;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Revisión interna de los vínculos filial–empresa matriz (M1-21, M8-04).</summary>
[ApiVersion("1.0")]
[Authorize]
[HasPermission(AccessPermissions.ReviewOrganizations)]
[Route("api/v{version:apiVersion}/admin/organization-links")]
public sealed class AdminOrganizationLinksController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetParentLinksQuery(status), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ParentLinkDecisionRequest? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ApproveParentLinkCommand(id, request?.Notes), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ParentLinkDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RejectParentLinkCommand(id, request.Reason ?? string.Empty), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record ParentLinkDecisionRequest(string? Notes, string? Reason);
