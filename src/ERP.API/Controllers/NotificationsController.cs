using ERP.API.Common;
using ERP.API.Contracts.Notifications;
using ERP.Application.Abstractions.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/notifications")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class NotificationsController(IUserNotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(
        [FromQuery] bool? isRead,
        CancellationToken cancellationToken)
    {
        var rows = await notificationService.GetAsync(isRead, cancellationToken);
        var result = rows.Select(x => new NotificationDto(
            x.Id, x.Type, x.Title, x.Message, x.IsRead, x.Link, x.CreatedAtUtc)).ToList();
        return Ok(result);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var updated = await notificationService.MarkAsReadAsync(id, cancellationToken);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await notificationService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateNotification([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest("Message is required.");

        var row = await notificationService.PublishAsync(
            request.Type,
            request.Title,
            request.Message,
            request.Link,
            cancellationToken);

        if (row is null)
        {
            return BadRequest("Notifications require an active tenant context.");
        }

        return Created($"/api/notifications/{row.Id}", row.Id);
    }
}
