using ERP.API.Common;
using ERP.API.Contracts.Announcements;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/platform-admin/announcements")]
[RequirePlatformAdmin]
public sealed class PlatformAdminAnnouncementsController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetAll(
        [FromQuery] string? q,
        [FromQuery] bool includeUnpublished = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Announcements.AsNoTracking().AsQueryable();

        if (!includeUnpublished)
        {
            query = query.Where(x => x.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(term) || x.Content.ToLower().Contains(term));
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);

        var items = await query
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new AnnouncementDto(
                x.Id,
                x.Title,
                x.Content,
                x.IsPublished,
                x.Priority,
                x.StartsAtUtc,
                x.EndsAtUtc,
                x.PublishedAtUtc,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Announcements
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AnnouncementDto(
                x.Id,
                x.Title,
                x.Content,
                x.IsPublished,
                x.Priority,
                x.StartsAtUtc,
                x.EndsAtUtc,
                x.PublishedAtUtc,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var validationResult = ValidateRequest(request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var entity = new Announcement
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            IsPublished = request.IsPublished,
            Priority = request.Priority,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            PublishedAtUtc = request.IsPublished ? DateTime.UtcNow : null
        };

        dbContext.Announcements.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/platform-admin/announcements/{entity.Id}", entity.Id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var validationResult = ValidateRequest(request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Title = request.Title.Trim();
        entity.Content = request.Content.Trim();
        entity.Priority = request.Priority;
        entity.StartsAtUtc = request.StartsAtUtc;
        entity.EndsAtUtc = request.EndsAtUtc;
        entity.IsPublished = request.IsPublished;
        entity.PublishedAtUtc = request.IsPublished
            ? entity.PublishedAtUtc ?? DateTime.UtcNow
            : null;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.IsPublished = true;
        entity.PublishedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.IsPublished = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NoContent();
        }

        entity.MarkAsDeleted();
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static BadRequestObjectResult? ValidateRequest(UpsertAnnouncementRequest request)
    {
        if (request is null)
        {
            return new BadRequestObjectResult("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return new BadRequestObjectResult("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return new BadRequestObjectResult("Content is required.");
        }

        if (request.Title.Trim().Length > 200)
        {
            return new BadRequestObjectResult("Title max length is 200.");
        }

        if (request.Content.Trim().Length > 4000)
        {
            return new BadRequestObjectResult("Content max length is 4000.");
        }

        if (request.EndsAtUtc.HasValue && request.StartsAtUtc.HasValue && request.EndsAtUtc < request.StartsAtUtc)
        {
            return new BadRequestObjectResult("EndsAtUtc cannot be earlier than StartsAtUtc.");
        }

        return null;
    }
}
