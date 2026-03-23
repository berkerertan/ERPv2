using ERP.API.Common;
using ERP.API.Contracts.Admin;
using ERP.API.Contracts.Auth;
using ERP.Application.Abstractions.Security;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/platform-admin")]
[RequirePlatformAdmin]
public sealed class PlatformAdminController(
    ErpDbContext dbContext,
    ISubscriptionPlanService subscriptionPlanService,
    IHostEnvironment hostEnvironment,
    IOptions<SecurityOptions> securityOptions) : ControllerBase
{
    [HttpGet("dashboard/overview")]
    [ProducesResponseType(typeof(AdminOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        var tenants = await dbContext.TenantAccounts.AsNoTracking().ToListAsync(cancellationToken);
        var users = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);

        var todayStart = DateTime.UtcNow.Date;
        var todayLogs = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= todayStart)
            .ToListAsync(cancellationToken);

        var activePlans = await subscriptionPlanService.GetAllPlansAsync(onlyActive: false, cancellationToken);
        var planLookup = activePlans.ToDictionary(x => x.Plan, x => x.MonthlyPrice);

        var totalMrr = tenants
            .Where(x => x.SubscriptionStatus == SubscriptionStatus.Active)
            .Sum(x => planLookup.TryGetValue(x.Plan, out var price) ? price : 0m);

        var overview = new AdminOverviewDto(
            tenants.Count,
            tenants.Count(x => x.SubscriptionStatus == SubscriptionStatus.Active),
            tenants.Count(x => x.SubscriptionStatus == SubscriptionStatus.Suspended),
            tenants.Count(x => x.SubscriptionStatus == SubscriptionStatus.Cancelled),
            users.Count,
            totalMrr,
            todayLogs.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().Count(),
            todayLogs.Count);

        return Ok(overview);
    }

    [HttpGet("audit-logs/summary")]
    [ProducesResponseType(typeof(AdminAuditLogSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminAuditLogSummaryDto>> GetAuditLogSummary(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAuditLogQuery(null, tenantId, userId, null, fromUtc, toUtc, onlyErrors);
        var logs = await query.ToListAsync(cancellationToken);

        var summary = new AdminAuditLogSummaryDto(
            logs.Count,
            logs.Count(x => x.StatusCode >= 400),
            logs.Count(x => x.OccurredAtUtc >= DateTime.UtcNow.Date),
            logs.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().Count(),
            logs.Where(x => x.TenantAccountId.HasValue).Select(x => x.TenantAccountId!.Value).Distinct().Count(),
            logs.Count == 0 ? 0d : Math.Round(logs.Average(x => x.DurationMs), 2));

        return Ok(summary);
    }

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminAuditLogDto>>> GetAuditLogs(
        [FromQuery] string? q,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? userId,
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAuditLogQuery(q, tenantId, userId, statusCode, fromUtc, toUtc, onlyErrors);
        var result = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 500))
            .Take(Math.Clamp(pageSize, 1, 500))
            .Select(x => new AdminAuditLogDto(x.Id, x.TenantAccountId, x.UserId, x.UserName, x.HttpMethod, x.Path, x.StatusCode, x.DurationMs, x.IpAddress, x.UserAgent, x.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("audit-logs/{id:guid}")]
    [ProducesResponseType(typeof(AdminAuditLogDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminAuditLogDto>> GetAuditLogById(Guid id, CancellationToken cancellationToken)
    {
        var log = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdminAuditLogDto(x.Id, x.TenantAccountId, x.UserId, x.UserName, x.HttpMethod, x.Path, x.StatusCode, x.DurationMs, x.IpAddress, x.UserAgent, x.OccurredAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    [HttpGet("subscribers")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminTenantListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminTenantListItemDto>>> GetSubscribers(
        [FromQuery] string? q,
        [FromQuery] SubscriptionPlan? plan,
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantQuery = dbContext.TenantAccounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            tenantQuery = tenantQuery.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term));
        }

        if (plan.HasValue)
        {
            tenantQuery = tenantQuery.Where(x => x.Plan == plan.Value);
        }

        if (status.HasValue)
        {
            tenantQuery = tenantQuery.Where(x => x.SubscriptionStatus == status.Value);
        }

        var tenants = await tenantQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 200))
            .Take(Math.Clamp(pageSize, 1, 200))
            .ToListAsync(cancellationToken);

        var tenantIds = tenants.Select(x => x.Id).ToList();

        var userCounts = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.TenantAccountId.HasValue && tenantIds.Contains(x.TenantAccountId.Value))
            .GroupBy(x => x.TenantAccountId!.Value)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var lastActivity = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.TenantAccountId.HasValue && tenantIds.Contains(x.TenantAccountId.Value))
            .GroupBy(x => x.TenantAccountId!.Value)
            .Select(g => new { TenantId = g.Key, LastAt = g.Max(x => x.OccurredAtUtc) })
            .ToDictionaryAsync(x => x.TenantId, x => (DateTime?)x.LastAt, cancellationToken);

        var plans = await subscriptionPlanService.GetAllPlansAsync(onlyActive: false, cancellationToken);
        var planPriceLookup = plans.ToDictionary(x => x.Plan, x => x.MonthlyPrice);
        var planRoleLookup = plans.ToDictionary(x => x.Plan, x => x.AssignedRole);

        var result = tenants
            .Select(x => new AdminTenantListItemDto(
                x.Id,
                x.Name,
                x.Code,
                x.Plan,
                planRoleLookup.TryGetValue(x.Plan, out var role) ? role : SubscriptionPlanCatalog.GetRoleForPlan(x.Plan),
                x.SubscriptionStatus,
                x.MaxUsers,
                userCounts.TryGetValue(x.Id, out var uc) ? uc : 0,
                x.SubscriptionStartAtUtc,
                x.SubscriptionEndAtUtc,
                lastActivity.TryGetValue(x.Id, out var la) ? la : null,
                planPriceLookup.TryGetValue(x.Plan, out var p) ? p : 0m))
            .ToList();

        return Ok(result);
    }

    [HttpGet("subscribers/{tenantId:guid}")]
    [ProducesResponseType(typeof(AdminTenantDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminTenantDetailDto>> GetSubscriberDetails(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.TenantAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        var userCount = await dbContext.Users.AsNoTracking().CountAsync(x => x.TenantAccountId == tenantId, cancellationToken);
        var plan = await subscriptionPlanService.GetPlanConfigAsync(tenant.Plan, cancellationToken);

        var activities = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.TenantAccountId == tenantId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(50)
            .Select(x => new AdminActivityLogDto(
                x.Id,
                x.TenantAccountId,
                x.UserId,
                x.UserName,
                x.HttpMethod,
                x.Path,
                x.StatusCode,
                x.DurationMs,
                x.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new AdminTenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.Code,
            tenant.Plan,
            plan.AssignedRole,
            tenant.SubscriptionStatus,
            tenant.MaxUsers,
            userCount,
            tenant.SubscriptionStartAtUtc,
            tenant.SubscriptionEndAtUtc,
            plan.MonthlyPrice,
            plan.Features,
            activities));
    }

    [HttpGet("subscribers/{tenantId:guid}/activities")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminActivityLogDto>>> GetSubscriberActivities(
        Guid tenantId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SystemActivityLogs.AsNoTracking().Where(x => x.TenantAccountId == tenantId);

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= toUtc.Value);
        }

        var result = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 500))
            .Take(Math.Clamp(pageSize, 1, 500))
            .Select(x => new AdminActivityLogDto(
                x.Id,
                x.TenantAccountId,
                x.UserId,
                x.UserName,
                x.HttpMethod,
                x.Path,
                x.StatusCode,
                x.DurationMs,
                x.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPut("subscribers/{tenantId:guid}/subscription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSubscriberSubscription(
        Guid tenantId,
        [FromBody] UpdateTenantSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.TenantAccounts.FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        var plan = await subscriptionPlanService.GetPlanConfigAsync(request.Plan, cancellationToken);
        tenant.Plan = request.Plan;
        tenant.SubscriptionStatus = request.Status;
        tenant.SubscriptionEndAtUtc = request.SubscriptionEndAtUtc;
        tenant.MaxUsers = plan.MaxUsers;
        tenant.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SubscriptionRoleSynchronization.ApplyTenantUserRolesAsync(dbContext, tenantId, cancellationToken);
        return NoContent();
    }

    [HttpGet("plans")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPlanOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanOptionDto>>> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await subscriptionPlanService.GetAllPlansAsync(onlyActive: false, cancellationToken);
        return Ok(plans.Select(x => new SubscriptionPlanOptionDto(
            x.Plan,
            x.DisplayName,
            x.AssignedRole,
            x.MonthlyPrice,
            x.MaxUsers,
            x.IsActive,
            x.Features)).ToList());
    }

    [HttpPut("plans/{plan}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePlan(SubscriptionPlan plan, [FromBody] UpdatePlanSettingRequest request, CancellationToken cancellationToken)
    {
        var setting = await dbContext.SubscriptionPlanSettings.FirstOrDefaultAsync(x => x.Plan == plan, cancellationToken);
        if (setting is null)
        {
            setting = new SubscriptionPlanSetting
            {
                Plan = plan,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? SubscriptionPlanCatalog.GetDisplayName(plan) : request.DisplayName.Trim(),
                MonthlyPrice = request.MonthlyPrice,
                MaxUsers = request.MaxUsers,
                IsActive = request.IsActive,
                FeaturesCsv = NormalizeFeatures(request.Features, plan)
            };

            dbContext.SubscriptionPlanSettings.Add(setting);
        }
        else
        {
            setting.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? SubscriptionPlanCatalog.GetDisplayName(plan) : request.DisplayName.Trim();
            setting.MonthlyPrice = request.MonthlyPrice;
            setting.MaxUsers = request.MaxUsers;
            setting.IsActive = request.IsActive;
            setting.FeaturesCsv = NormalizeFeatures(request.Features, plan);
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("landing-content")]
    [ProducesResponseType(typeof(IReadOnlyList<LandingPageContentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LandingPageContentDto>>> GetLandingContent(CancellationToken cancellationToken)
    {
        var items = await dbContext.LandingPageContents
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Key)
            .Select(x => new LandingPageContentDto(
                x.Key,
                x.Title,
                x.Content,
                x.IsPublished,
                x.SortOrder,
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPut("landing-content/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertLandingContent(string key, [FromBody] UpdateLandingPageContentRequest request, CancellationToken cancellationToken)
    {
        var normalizedKey = (key ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return BadRequest("Content key is required.");
        }

        var existing = await dbContext.LandingPageContents.FirstOrDefaultAsync(x => x.Key == normalizedKey, cancellationToken);
        if (existing is null)
        {
            dbContext.LandingPageContents.Add(new LandingPageContent
            {
                Key = normalizedKey,
                Title = request.Title,
                Content = request.Content,
                IsPublished = request.IsPublished,
                SortOrder = request.SortOrder
            });
        }
        else
        {
            existing.Title = request.Title;
            existing.Content = request.Content;
            existing.IsPublished = request.IsPublished;
            existing.SortOrder = request.SortOrder;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("analytics/revenue")]
    [ProducesResponseType(typeof(AdminRevenueSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminRevenueSummaryDto>> GetRevenueAnalytics(CancellationToken cancellationToken)
    {
        var activeTenants = await dbContext.TenantAccounts
            .AsNoTracking()
            .Where(x => x.SubscriptionStatus == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);

        var plans = await subscriptionPlanService.GetAllPlansAsync(onlyActive: false, cancellationToken);
        var planLookup = plans.ToDictionary(x => x.Plan, x => x);

        var breakdown = activeTenants
            .GroupBy(x => x.Plan)
            .Select(g =>
            {
                var plan = planLookup.TryGetValue(g.Key, out var cfg) ? cfg : null;
                var monthlyPrice = plan?.MonthlyPrice ?? 0m;
                var count = g.Count();

                return new AdminRevenuePointDto(
                    g.Key.ToString(),
                    count,
                    monthlyPrice,
                    count * monthlyPrice);
            })
            .OrderBy(x => x.Plan)
            .ToList();

        return Ok(new AdminRevenueSummaryDto(
            breakdown.Sum(x => x.Revenue),
            breakdown));
    }

    [HttpGet("system-health/overview")]
    [ProducesResponseType(typeof(AdminSystemHealthOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminSystemHealthOverviewDto>> GetSystemHealthOverview(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var todayStart = now.Date;

        var dbCheckStarted = Stopwatch.StartNew();
        var databaseReachable = await dbContext.Database.CanConnectAsync(cancellationToken);
        dbCheckStarted.Stop();

        var lastHourLogs = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= oneHourAgo)
            .ToListAsync(cancellationToken);

        var todayLogs = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= todayStart)
            .ToListAsync(cancellationToken);

        var requestsLastHour = lastHourLogs.Count;
        var errorsLastHour = lastHourLogs.Count(x => x.StatusCode >= 400);
        var errorRateLastHour = requestsLastHour == 0
            ? 0d
            : Math.Round((double)errorsLastHour / requestsLastHour * 100d, 2);

        var averageDurationMs = requestsLastHour == 0
            ? 0d
            : Math.Round(lastHourLogs.Average(x => x.DurationMs), 2);

        var status = BuildSystemHealthStatus(databaseReachable, errorRateLastHour);
        var processStartUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

        var response = new AdminSystemHealthOverviewDto(
            status,
            now,
            processStartUtc,
            Math.Round((now - processStartUtc).TotalSeconds, 0),
            hostEnvironment.EnvironmentName,
            version,
            securityOptions.Value.EnforceAuthorization,
            databaseReachable,
            requestsLastHour,
            errorsLastHour,
            errorRateLastHour,
            averageDurationMs,
            todayLogs.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().Count(),
            todayLogs.Where(x => x.TenantAccountId.HasValue).Select(x => x.TenantAccountId!.Value).Distinct().Count(),
            todayLogs.OrderByDescending(x => x.OccurredAtUtc).Select(x => (DateTime?)x.OccurredAtUtc).FirstOrDefault(),
            todayLogs.Where(x => x.StatusCode >= 400).OrderByDescending(x => x.OccurredAtUtc).Select(x => (DateTime?)x.OccurredAtUtc).FirstOrDefault());

        return Ok(response);
    }

    [HttpGet("system-health/dependencies")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminSystemDependencyStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminSystemDependencyStatusDto>>> GetSystemHealthDependencies(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var checks = new List<AdminSystemDependencyStatusDto>();

        var databaseWatch = Stopwatch.StartNew();
        var databaseReachable = await dbContext.Database.CanConnectAsync(cancellationToken);
        databaseWatch.Stop();

        checks.Add(new AdminSystemDependencyStatusDto(
            "database",
            databaseReachable ? "Healthy" : "Unhealthy",
            databaseWatch.ElapsedMilliseconds,
            databaseReachable ? "SQL connection established." : "SQL connection failed.",
            now));

        checks.Add(new AdminSystemDependencyStatusDto(
            "authorization",
            securityOptions.Value.EnforceAuthorization ? "Healthy" : "Degraded",
            0,
            securityOptions.Value.EnforceAuthorization
                ? "Authorization enforcement is active."
                : "Authorization enforcement is disabled.",
            now));

        checks.Add(new AdminSystemDependencyStatusDto(
            "api",
            "Healthy",
            0,
            "API process is responding to the admin health endpoint.",
            now));

        return Ok(checks);
    }

    [HttpGet("system-health/timeline")]
    [ProducesResponseType(typeof(AdminSystemHealthTimelineDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminSystemHealthTimelineDto>> GetSystemHealthTimeline(
        [FromQuery] int minutes = 60,
        [FromQuery] int bucketMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        var safeMinutes = Math.Clamp(minutes, 15, 24 * 60);
        var safeBucketMinutes = Math.Clamp(bucketMinutes, 1, 60);
        var now = DateTime.UtcNow;
        var rangeStart = now.AddMinutes(-safeMinutes);

        var logs = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= rangeStart)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        var buckets = new List<AdminSystemHealthTimelinePointDto>();
        var bucketStart = TruncateToBucket(rangeStart, safeBucketMinutes);
        var rangeEnd = now;

        while (bucketStart <= rangeEnd)
        {
            var bucketEnd = bucketStart.AddMinutes(safeBucketMinutes);
            var bucketLogs = logs
                .Where(x => x.OccurredAtUtc >= bucketStart && x.OccurredAtUtc < bucketEnd)
                .ToList();

            buckets.Add(new AdminSystemHealthTimelinePointDto(
                bucketStart,
                bucketLogs.Count,
                bucketLogs.Count(x => x.StatusCode >= 400),
                bucketLogs.Count == 0 ? 0d : Math.Round(bucketLogs.Average(x => x.DurationMs), 2)));

            bucketStart = bucketEnd;
        }

        return Ok(new AdminSystemHealthTimelineDto(
            safeMinutes,
            safeBucketMinutes,
            buckets));
    }

    private IQueryable<SystemActivityLog> BuildAuditLogQuery(
        string? q,
        Guid? tenantId,
        Guid? userId,
        int? statusCode,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool onlyErrors)
    {
        var query = dbContext.SystemActivityLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x =>
                x.Path.ToLower().Contains(term) ||
                (x.UserName != null && x.UserName.ToLower().Contains(term)) ||
                (x.IpAddress != null && x.IpAddress.ToLower().Contains(term)));
        }

        if (tenantId.HasValue)
        {
            query = query.Where(x => x.TenantAccountId == tenantId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (statusCode.HasValue)
        {
            query = query.Where(x => x.StatusCode == statusCode.Value);
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

    private static AdminAuditLogDto MapAuditLog(SystemActivityLog x)
        => new(
            x.Id,
            x.TenantAccountId,
            x.UserId,
            x.UserName,
            x.HttpMethod,
            x.Path,
            x.StatusCode,
            x.DurationMs,
            x.IpAddress,
            x.UserAgent,
            x.OccurredAtUtc);

    private static string NormalizeFeatures(List<string> features, SubscriptionPlan plan)
    {
        var normalized = (features ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            normalized = SubscriptionPlanCatalog.GetFeatures(plan).ToList();
        }

        return string.Join(',', normalized);
    }

    private static string BuildSystemHealthStatus(bool databaseReachable, double errorRateLastHour)
    {
        if (!databaseReachable)
        {
            return "Unhealthy";
        }

        if (errorRateLastHour >= 10d)
        {
            return "Degraded";
        }

        return "Healthy";
    }

    private static DateTime TruncateToBucket(DateTime value, int bucketMinutes)
    {
        var totalMinutes = (value.Minute / bucketMinutes) * bucketMinutes;
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, totalMinutes, 0, DateTimeKind.Utc);
    }
}






