namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Admin.Organizations;
using HapagPortal.Application.Organizations.Documents;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Flujo interno de creación de clientes y asignación de Match Code (M8-04): validación del cliente,
/// punto de control con AR y rechazo. Solo perfiles internos de Hapag-Lloyd.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/admin/organizations")]
public sealed class AdminOrganizationsController : ApiController
{
    [HttpGet]
    [HasPermission(PermissionLogic.Any, AccessPermissions.ReviewOrganizations, AccessPermissions.CheckOrganizationsAr)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionLogic.Any, AccessPermissions.ReviewOrganizations, AccessPermissions.CheckOrganizationsAr)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOrganizationReviewQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}")]
    [HasPermission(PermissionLogic.Any, AccessPermissions.ReviewOrganizations, AccessPermissions.CheckOrganizationsAr)]
    public async Task<IActionResult> DownloadDocument(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOrganizationDocumentContentQuery(id, documentId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/validate")]
    [HasPermission(AccessPermissions.ReviewOrganizations)]
    public async Task<IActionResult> Validate(
        Guid id,
        [FromBody] ValidateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("{id:guid}/ar-check")]
    [HasPermission(AccessPermissions.CheckOrganizationsAr)]
    public async Task<IActionResult> CompleteArCheck(
        Guid id,
        [FromBody] CompleteOrganizationArCheckCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("{id:guid}/reject")]
    [HasPermission(AccessPermissions.ReviewOrganizations)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
