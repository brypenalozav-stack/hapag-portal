namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Assistant;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Asistente del portal (M10-01 a M10-03, M10-05): conversaciones del usuario con su historial, respuestas desde
/// la base de conocimiento del país y desde los datos del portal con sus permisos, y respaldo por correo al
/// cerrar. Base de conocimiento y casillas de derivación administrables desde el portal interno (M10-02, NF-15).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/assistant")]
public sealed class AssistantController : ApiController
{
    [HttpPost("sessions")]
    public async Task<IActionResult> Start(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new StartAssistantSessionCommand(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAssistantSessionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("sessions/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] AssistantMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SendAssistantMessageCommand(id, request.Message ?? string.Empty), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Descarga de un documento entregado por el asistente (M10-04): vuelve a validar los permisos del usuario y registra la
    /// descarga con el canal <c>Assistant</c> (NF-14).
    /// </summary>
    [HttpGet("sessions/{id:guid}/deliveries/{deliveryId:guid}/download")]
    public async Task<IActionResult> DownloadDelivery(Guid id, Guid deliveryId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DownloadAssistantDeliveryCommand(id, deliveryId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    /// <summary>Cierra la conversación y, si se pide, envía el respaldo al correo registrado o al indicado (M10-05).</summary>
    [HttpPost("sessions/{id:guid}/end")]
    public async Task<IActionResult> End(
        Guid id,
        [FromBody] EndAssistantSessionRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new EndAssistantSessionCommand(id, request?.SendTranscript ?? false, request?.Email), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("knowledge")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetKnowledge(
        [FromQuery] string? country,
        [FromQuery] string? topic,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetKnowledgeArticlesQuery(country, topic, includeInactive), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("knowledge/{id:guid}/history")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetKnowledgeHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetKnowledgeArticleHistoryQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("knowledge")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> CreateKnowledge([FromBody] KnowledgeArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateKnowledgeArticleCommand(
            request.Country, request.Topic, request.Title, request.Content, request.Keywords, request.SortOrder), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("knowledge/{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> UpdateKnowledge(Guid id, [FromBody] KnowledgeArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateKnowledgeArticleCommand(
            id, request.Country, request.Topic, request.Title, request.Content, request.Keywords, request.SortOrder), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("knowledge/{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> DeactivateKnowledge(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateKnowledgeArticleCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("mailboxes")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetMailboxes(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAssistantMailboxesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("mailboxes")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> UpsertMailbox([FromBody] UpsertAssistantMailboxCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record AssistantMessageRequest(string? Message);

public sealed record EndAssistantSessionRequest(bool SendTranscript, string? Email);

public sealed record KnowledgeArticleRequest(
    string Country,
    string Topic,
    string Title,
    string Content,
    string? Keywords,
    int SortOrder);
