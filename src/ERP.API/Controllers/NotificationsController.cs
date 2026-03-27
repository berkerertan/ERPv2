using ERP.API.Common;
using ERP.API.Contracts.Notifications;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/notifications")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class NotificationsController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(
        [FromQuery] bool? isRead,
        CancellationToken cancellationToken)
    {
        var query = dbContext.UserNotifications.AsNoTracking().AsQueryable();
        if (isRead.HasValue) query = query.Where(x => x.IsRead == isRead.Value);

        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

        var result = rows.Select(x => new NotificationDto(
            x.Id, x.Type, x.Title, x.Message, x.IsRead, x.Link, x.CreatedAtUtc)).ToList();

        return Ok(result);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.UserNotifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        row.IsRead = true;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var unread = await dbContext.UserNotifications
            .Where(x => !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.UserNotifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateNotification([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest("Message is required.");

        var row = new UserNotification
        {
            Type = request.Type.Trim(),
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Link = request.Link?.Trim(),
            IsRead = false
        };

        dbContext.UserNotifications.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/notifications/{row.Id}", row.Id);
    }
}
