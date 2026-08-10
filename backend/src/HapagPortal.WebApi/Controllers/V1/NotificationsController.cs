namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Notifications.GetMy;
using HapagPortal.Application.Notifications.MarkAllRead;
using HapagPortal.Application.Notifications.MarkRead;
using HapagPortal.Application.Notifications.UnreadCount;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool onlyUnread, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMyNotificationsQuery(onlyUnread), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var result = await Sender.Send(new GetUnreadCountQuery(), ct);
        return result.IsSuccess ? Ok(new { count = result.Value }) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var result = await Sender.Send(new MarkAllNotificationsReadCommand(), ct);
        return result.IsSuccess ? Ok(new { marked = result.Value }) : HandleFailure(result);
    }
}
