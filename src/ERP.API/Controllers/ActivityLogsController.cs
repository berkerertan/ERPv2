using ERP.API.Common;
using ERP.API.Contracts.ActivityLogs;
using ERP.Application.Abstractions.Security;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/activity-logs")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class ActivityLogsController(
    ErpDbContext dbContext,
    ICurrentTenantService currentTenantService) : ControllerBase
{
    [HttpGet("me/summary")]
    [ProducesResponseType(typeof(MyActivityLogSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyActivityLogSummaryDto>> GetMySummary(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var logs = await BuildMyLogQuery(userId, fromUtc, toUtc, onlyErrors)
            .ToListAsync(cancellationToken);

        var summary = new MyActivityLogSummaryDto(
            logs.Count,
            logs.Count(x => x.StatusCode >= 400),
            logs.Count(x => x.OccurredAtUtc >= DateTime.UtcNow.Date),
            logs.Count == 0 ? 0d : Math.Round(logs.Average(x => x.DurationMs), 2),
            logs.Count == 0 ? null : logs.Max(x => x.OccurredAtUtc));

        return Ok(summary);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<MyActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyActivityLogDto>>> GetMyLogs(
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = BuildMyLogQuery(userId, fromUtc, toUtc, onlyErrors);

        if (statusCode.HasValue)
        {
            query = query.Where(x => x.StatusCode == statusCode.Value);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 500);

        var result = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new MyActivityLogDto(
                x.Id,
                x.TenantAccountId,
                x.UserId,
                x.HttpMethod,
                x.Path,
                x.StatusCode,
                x.DurationMs,
                x.IpAddress,
                x.UserAgent,
                x.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    private IQueryable<SystemActivityLog> BuildMyLogQuery(Guid userId, DateTime? fromUtc, DateTime? toUtc, bool onlyErrors)
    {
        var query = dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (currentTenantService.TenantId.HasValue)
        {
            query = query.Where(x => x.TenantAccountId == currentTenantService.TenantId.Value);
        }
        else
        {
            query = query.Where(x => x.TenantAccountId == null);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= toUtc.Value);
        }

        if (onlyErrors)
        {
            query = query.Where(x => x.StatusCode >= 400);
        }

        return query;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out userId);
    }
}
