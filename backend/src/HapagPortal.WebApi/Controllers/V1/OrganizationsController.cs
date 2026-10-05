namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Organizations.Documents;
using HapagPortal.Application.Organizations.GetMine;
using HapagPortal.Application.Organizations.JoinRequests;
using HapagPortal.Application.Organizations.OperatingCountry;
using HapagPortal.Application.Organizations.Users;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Organización del usuario autenticado (M1-02, M1-04, M1-07, M1-08). Todas las operaciones quedan
/// acotadas a la propia organización en Application; los permisos del perfil se exigen aquí.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/organizations")]
public sealed class OrganizationsController : ApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyOrganizationQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("me/users")]
    [HasPermission(AccessPermissions.ManageOrganizationUsers)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOrganizationUsersQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("me/users")]
    [HasPermission(AccessPermissions.ManageOrganizationUsers)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateOrganizationUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(CreateUser), result.Value)
            : HandleFailure(result);
    }

    [HttpPut("me/users/{id:guid}")]
    [HasPermission(AccessPermissions.ManageOrganizationUsers)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateOrganizationUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("me/users/{id:guid}/active")]
    [HasPermission(AccessPermissions.ManageOrganizationUsers)]
    public async Task<IActionResult> SetUserActive(
        Guid id,
        [FromBody] SetOrganizationUserActiveCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("me/join-requests")]
    [HasPermission(AccessPermissions.ApproveJoinRequests)]
    public async Task<IActionResult> GetJoinRequests(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetJoinRequestsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("me/join-requests/{userId:guid}/approve")]
    [HasPermission(AccessPermissions.ApproveJoinRequests)]
    public async Task<IActionResult> ApproveJoinRequest(
        Guid userId,
        [FromBody] ApproveJoinRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { UserId = userId }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("me/join-requests/{userId:guid}/reject")]
    [HasPermission(AccessPermissions.ApproveJoinRequests)]
    public async Task<IActionResult> RejectJoinRequest(
        Guid userId,
        [FromBody] RejectJoinRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { UserId = userId }, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("me/operating-country")]
    public async Task<IActionResult> GetOperatingCountry(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOperatingCountryQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("me/operating-country")]
    public async Task<IActionResult> SetOperatingCountry(
        [FromBody] SetOperatingCountryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("me/documents")]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyOrganizationDocumentsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Adjunta documentación de respaldo del registro (multipart: file, documentType).</summary>
    [HttpPost("me/documents")]
    [RequestSizeLimit(UploadOrganizationDocumentCommandValidator.MaxSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] string documentType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var command = new UploadOrganizationDocumentCommand(
            documentType,
            file.FileName,
            file.ContentType,
            buffer.ToArray());

        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDocuments), result.Value)
            : HandleFailure(result);
    }
}
