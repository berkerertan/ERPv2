using ERP.API.Common;
using ERP.API.Contracts.Announcements;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/announcements")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class AnnouncementsController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetActive(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = dbContext.Announcements
            .AsNoTracking()
            .Where(x =>
                x.IsPublished &&
                (!x.StartsAtUtc.HasValue || x.StartsAtUtc <= now) &&
                (!x.EndsAtUtc.HasValue || x.EndsAtUtc >= now));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(term) || x.Content.ToLower().Contains(term));
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(MapDtoExpression())
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetActiveById(Guid id, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var item = await dbContext.Announcements
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.IsPublished &&
                (!x.StartsAtUtc.HasValue || x.StartsAtUtc <= now) &&
                (!x.EndsAtUtc.HasValue || x.EndsAtUtc >= now))
            .Select(MapDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    private static System.Linq.Expressions.Expression<Func<ERP.Domain.Entities.Announcement, AnnouncementDto>> MapDtoExpression()
    {
        return x => new AnnouncementDto(
            x.Id,
            x.Title,
            x.Content,
            x.IsPublished,
            x.Priority,
            x.StartsAtUtc,
            x.EndsAtUtc,
            x.PublishedAtUtc,
            x.CreatedAtUtc,
            x.UpdatedAtUtc);
    }
}
