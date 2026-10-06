namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Documents.ReleaseLetter;
using HapagPortal.Application.ServiceRequests.Queue;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Bandeja interna de solicitudes de servicios on demand para los equipos ED y Customer Service: aprobar o
/// rechazar (Drop Off, M3-09), completar la prestación después del pago, notas y documento de salida.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/admin/service-requests")]
public sealed class AdminServiceRequestsController : ApiController
{
    [HttpGet]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> GetQueue(
        [FromQuery] string? status,
        [FromQuery] string? team,
        [FromQuery] string? definitionCode,
        [FromQuery] string? country,
        [FromQuery] string? blNumber,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Sender.Send(
            new GetServiceRequestQueueQuery(status, team, definitionCode, country, blNumber, organizationId, page, pageSize),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInternalServiceRequestQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Carta de liberación y desconsolidado en revisión (M6-08): TATC registrado al enviar, TATC consultado ahora, registro de
    /// Counter (M8-09) y regla de TATC vigente. Aprobar la solicitud emite la carta.
    /// </summary>
    [HttpGet("{id:guid}/release-letter")]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> GetReleaseLetter(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInternalReleaseLetterRequestQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/approve")]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ServiceRequestNotesBody? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ApproveServiceRequestCommand(id, request?.Notes), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/reject")]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectServiceRequestBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RejectServiceRequestCommand(id, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/complete")]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> Complete(Guid id, [FromBody] ServiceRequestNotesBody? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CompleteServiceRequestCommand(id, request?.Notes), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/notes")]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] ServiceRequestNotesBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AddServiceRequestNoteCommand(id, request.Notes ?? string.Empty), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Documento de salida del equipo (multipart: file).</summary>
    [HttpPost("{id:guid}/attachments")]
    [HasPermission(ServiceRequestPermissions.Process)]
    [RequestSizeLimit(UploadServiceRequestAttachmentCommandValidator.MaxSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadOutput(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var result = await Sender.Send(
            new UploadServiceRequestOutputCommand(id, file.FileName, file.ContentType, buffer.ToArray()), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [HasPermission(ServiceRequestPermissions.Process)]
    public async Task<IActionResult> Download(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInternalServiceRequestAttachmentQuery(id, attachmentId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }
}

public sealed record ServiceRequestNotesBody(string? Notes);

public sealed record RejectServiceRequestBody(string Reason);
