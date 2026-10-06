namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Notifications.GetMy;
using HapagPortal.Application.Notifications.MarkAllRead;
using HapagPortal.Application.Notifications.MarkRead;
using HapagPortal.Application.Notifications.Preferences;
using HapagPortal.Application.Notifications.UnreadCount;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Bandeja de notificaciones del portal (M1-25): notificaciones de todos los módulos con la gestión o el embarque al que
/// corresponden y la acción disponible, filtros por tipo y módulo, y la preferencia de correo por tipo de notificación.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] bool onlyUnread,
        [FromQuery] string? type,
        [FromQuery] string? module,
        [FromQuery] string? blNumber,
        [FromQuery] bool onlyActionable,
        CancellationToken ct,
        [FromQuery] int limit = 100)
    {
        var result = await Sender.Send(new GetMyNotificationsQuery(onlyUnread, type, module, blNumber, onlyActionable, limit), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var result = await Sender.Send(new GetUnreadSummaryQuery(), ct);
        return result.IsSuccess
            ? Ok(new { count = result.Value.Count, byModule = result.Value.ByModule, actionable = result.Value.Actionable })
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead([FromQuery] string? module, CancellationToken ct)
    {
        var result = await Sender.Send(new MarkAllNotificationsReadCommand(module), ct);
        return result.IsSuccess ? Ok(new { marked = result.Value }) : HandleFailure(result);
    }

    /// <summary>Qué tipos de notificación recibe además por correo el usuario (M1-25).</summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await Sender.Send(new GetNotificationPreferencesQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary><c>emailEnabled</c> nulo vuelve al valor por defecto del tipo.</summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferencesCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
