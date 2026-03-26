using ERP.API.Common;
using ERP.API.Contracts.Admin;
using ERP.Application.Abstractions.Notifications;
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
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/platform-admin")]
[RequirePlatformAdmin]
public sealed class PlatformAdminController(
    ErpDbContext dbContext,
    ISubscriptionPlanService subscriptionPlanService,
    IEmailSender emailSender,
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

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminUserListItemDto>>> GetUsers(
        [FromQuery] string? q,
        [FromQuery] Guid? tenantId,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.UserName.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term) ||
                x.Role.ToLower().Contains(term));
        }

        if (tenantId.HasValue)
        {
            query = query.Where(x => x.TenantAccountId == tenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(x => x.Role.ToLower() == normalizedRole);
        }

        if (isActive.HasValue)
        {
            query = isActive.Value
                ? query.Where(x => !x.IsDeleted)
                : query.Where(x => x.IsDeleted);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);

        var users = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var tenantIds = users
            .Where(x => x.TenantAccountId.HasValue)
            .Select(x => x.TenantAccountId!.Value)
            .Distinct()
            .ToList();

        var tenantLookup = await dbContext.TenantAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => tenantIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Code })
            .ToDictionaryAsync(x => x.Id, x => (x.Name, x.Code), cancellationToken);

        var userIds = users.Select(x => x.Id).ToList();
        var lastLoginLookup = await dbContext.UserSessions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, LastLoginAtUtc = g.Max(v => (DateTime?)v.LastActiveUtc) })
            .ToDictionaryAsync(x => x.UserId, x => x.LastLoginAtUtc, cancellationToken);

        var result = users
            .Select(x =>
            {
                var tenantInfo = x.TenantAccountId.HasValue && tenantLookup.TryGetValue(x.TenantAccountId.Value, out var t)
                    ? t
                    : ((string Name, string Code)?)null;

                return new AdminUserListItemDto(
                    x.Id,
                    x.UserName,
                    x.Email,
                    x.Role,
                    x.TenantAccountId,
                    tenantInfo?.Name,
                    tenantInfo?.Code,
                    !x.IsDeleted,
                    x.CreatedAtUtc,
                    lastLoginLookup.TryGetValue(x.Id, out var lastLoginAtUtc) ? lastLoginAtUtc : null);
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUserDetailDto>> GetUserDetail(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var tenant = user.TenantAccountId.HasValue
            ? await dbContext.TenantAccounts
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == user.TenantAccountId.Value, cancellationToken)
            : null;

        var lastLoginAtUtc = await dbContext.UserSessions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (DateTime?)x.LastActiveUtc)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var recentActivities = await dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
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

        return Ok(new AdminUserDetailDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role,
            user.TenantAccountId,
            tenant?.Name,
            tenant?.Code,
            !user.IsDeleted,
            user.CreatedAtUtc,
            lastLoginAtUtc,
            tenant?.Plan,
            tenant?.SubscriptionStatus,
            recentActivities));
    }

    [HttpPost("users/{userId:guid}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ToggleUserActive(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId(User);
        if (currentUserId == userId)
        {
            return BadRequest("Current platform admin user cannot deactivate itself.");
        }

        if (user.IsDeleted)
        {
            var userEntry = dbContext.Entry(user);
            userEntry.Property(nameof(AppUser.IsDeleted)).CurrentValue = false;
            userEntry.Property(nameof(AppUser.DeletedAtUtc)).CurrentValue = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            user.MarkAsDeleted(DateTime.UtcNow);
            user.RefreshToken = null;
            user.RefreshTokenExpiresAtUtc = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

    [HttpGet("email/templates")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminEmailTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminEmailTemplateDto>>> GetEmailTemplates(CancellationToken cancellationToken)
    {
        var templates = await dbContext.PlatformEmailTemplates
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new AdminEmailTemplateDto(
                x.Id,
                x.Key,
                x.Name,
                x.SubjectTemplate,
                x.BodyTemplate,
                x.Description,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(templates);
    }

    [HttpGet("email/campaigns")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminEmailCampaignDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminEmailCampaignDto>>> GetEmailCampaigns(
        [FromQuery] string? q,
        [FromQuery] PlatformEmailCampaignStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PlatformEmailCampaigns.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.TemplateKey.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);

        var campaigns = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return Ok(campaigns.Select(MapCampaign).ToList());
    }

    [HttpGet("email/campaigns/{campaignId:guid}")]
    [ProducesResponseType(typeof(AdminEmailCampaignDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminEmailCampaignDto>> GetEmailCampaignById(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.PlatformEmailCampaigns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return NotFound();
        }

        return Ok(MapCampaign(campaign));
    }

    [HttpGet("email/campaigns/{campaignId:guid}/recipients")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminEmailCampaignRecipientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminEmailCampaignRecipientDto>>> GetEmailCampaignRecipients(
        Guid campaignId,
        [FromQuery] PlatformEmailRecipientStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var campaignExists = await dbContext.PlatformEmailCampaigns.AsNoTracking()
            .AnyAsync(x => x.Id == campaignId, cancellationToken);

        if (!campaignExists)
        {
            return NotFound();
        }

        var query = dbContext.PlatformEmailCampaignRecipients.AsNoTracking()
            .Where(x => x.CampaignId == campaignId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 500);

        var recipients = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new AdminEmailCampaignRecipientDto(
                x.Id,
                x.CampaignId,
                x.TenantAccountId,
                x.TenantCode,
                x.TenantName,
                x.RecipientEmail,
                x.Status,
                x.AttemptCount,
                x.NextAttemptAtUtc,
                x.LastAttemptedAtUtc,
                x.SentAtUtc,
                x.ProviderMessage))
            .ToListAsync(cancellationToken);

        return Ok(recipients);
    }

    [HttpPost("email/campaigns/preview")]
    [ProducesResponseType(typeof(AdminEmailCampaignAudiencePreviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminEmailCampaignAudiencePreviewDto>> PreviewEmailCampaignAudience(
        [FromBody] AdminEmailCampaignPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateKey = NormalizeTemplateKey(request.TemplateKey);
        if (string.IsNullOrWhiteSpace(normalizedTemplateKey))
        {
            return BadRequest("TemplateKey is required.");
        }

        var templateExists = await dbContext.PlatformEmailTemplates.AsNoTracking()
            .AnyAsync(x => x.Key == normalizedTemplateKey && x.IsActive, cancellationToken);
        if (!templateExists)
        {
            return NotFound($"Active template '{normalizedTemplateKey}' was not found.");
        }

        var audience = await ResolveAudienceAsync(
            request.SendToAllActiveTenants,
            request.TenantIds,
            request.SendToAllTenantUsers,
            cancellationToken);

        return Ok(new AdminEmailCampaignAudiencePreviewDto(
            audience.TenantIds.Count,
            audience.Recipients.Count,
            audience.Recipients
                .Take(50)
                .Select(x => new AdminEmailCampaignAudienceRecipientDto(
                    x.TenantId,
                    x.TenantCode,
                    x.TenantName,
                    x.RecipientEmail))
                .ToList()));
    }

    [HttpPost("email/campaigns")]
    [ProducesResponseType(typeof(AdminEmailCampaignDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminEmailCampaignDto>> CreateEmailCampaignDraft(
        [FromBody] AdminEmailCampaignDraftRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateKey = NormalizeTemplateKey(request.TemplateKey);
        if (string.IsNullOrWhiteSpace(normalizedTemplateKey))
        {
            return BadRequest("TemplateKey is required.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Campaign name is required.");
        }

        if (name.Length > 150 || (request.Description?.Length ?? 0) > 500)
        {
            return BadRequest("Campaign fields exceed allowed length.");
        }

        var template = await dbContext.PlatformEmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == normalizedTemplateKey && x.IsActive, cancellationToken);
        if (template is null)
        {
            return NotFound($"Active template '{normalizedTemplateKey}' was not found.");
        }

        var subject = (request.SubjectOverride ?? template.SubjectTemplate ?? string.Empty).Trim();
        var body = (request.BodyOverride ?? template.BodyTemplate ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
        {
            return BadRequest("Subject and body cannot be empty.");
        }

        if (subject.Length > 300 || body.Length > 8000)
        {
            return BadRequest("Subject or body exceeds allowed length.");
        }

        var audience = await ResolveAudienceAsync(
            request.SendToAllActiveTenants,
            request.TenantIds,
            request.SendToAllTenantUsers,
            cancellationToken);

        var campaign = new PlatformEmailCampaign
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            TemplateKey = normalizedTemplateKey,
            SubjectTemplate = subject,
            BodyTemplate = body,
            IsHtml = request.IsHtml,
            SendToAllActiveTenants = request.SendToAllActiveTenants,
            SendToAllTenantUsers = request.SendToAllTenantUsers,
            TenantIdsJson = SerializeGuidList(audience.TenantIds),
            VariablesJson = SerializeVariables(request.Variables),
            ScheduledAtUtc = request.ScheduledAtUtc,
            Status = PlatformEmailCampaignStatus.Draft,
            TotalRecipients = audience.Recipients.Count,
            CreatedByUserId = GetCurrentUserId(User),
            CreatedByUserName = User.Identity?.Name
        };

        dbContext.PlatformEmailCampaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapCampaign(campaign);
        return CreatedAtAction(nameof(GetEmailCampaignById), new { campaignId = campaign.Id }, dto);
    }

    [HttpPut("email/campaigns/{campaignId:guid}")]
    [ProducesResponseType(typeof(AdminEmailCampaignDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminEmailCampaignDto>> UpdateEmailCampaignDraft(
        Guid campaignId,
        [FromBody] AdminEmailCampaignDraftRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = await dbContext.PlatformEmailCampaigns
            .FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status != PlatformEmailCampaignStatus.Draft)
        {
            return BadRequest("Only draft campaigns can be updated.");
        }

        var normalizedTemplateKey = NormalizeTemplateKey(request.TemplateKey);
        if (string.IsNullOrWhiteSpace(normalizedTemplateKey))
        {
            return BadRequest("TemplateKey is required.");
        }

        var template = await dbContext.PlatformEmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == normalizedTemplateKey && x.IsActive, cancellationToken);
        if (template is null)
        {
            return NotFound($"Active template '{normalizedTemplateKey}' was not found.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Campaign name is required.");
        }

        var subject = (request.SubjectOverride ?? template.SubjectTemplate ?? string.Empty).Trim();
        var body = (request.BodyOverride ?? template.BodyTemplate ?? string.Empty).Trim();

        if (name.Length > 150 || subject.Length > 300 || body.Length > 8000 || (request.Description?.Length ?? 0) > 500)
        {
            return BadRequest("Campaign fields exceed allowed length.");
        }

        var audience = await ResolveAudienceAsync(
            request.SendToAllActiveTenants,
            request.TenantIds,
            request.SendToAllTenantUsers,
            cancellationToken);

        campaign.Name = name;
        campaign.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        campaign.TemplateKey = normalizedTemplateKey;
        campaign.SubjectTemplate = subject;
        campaign.BodyTemplate = body;
        campaign.IsHtml = request.IsHtml;
        campaign.SendToAllActiveTenants = request.SendToAllActiveTenants;
        campaign.SendToAllTenantUsers = request.SendToAllTenantUsers;
        campaign.TenantIdsJson = SerializeGuidList(audience.TenantIds);
        campaign.VariablesJson = SerializeVariables(request.Variables);
        campaign.ScheduledAtUtc = request.ScheduledAtUtc;
        campaign.TotalRecipients = audience.Recipients.Count;
        campaign.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(MapCampaign(campaign));
    }

    [HttpPost("email/campaigns/{campaignId:guid}/queue")]
    [ProducesResponseType(typeof(AdminEmailCampaignQueueResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminEmailCampaignQueueResultDto>> QueueEmailCampaign(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var campaign = await dbContext.PlatformEmailCampaigns
            .FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status is not PlatformEmailCampaignStatus.Draft and not PlatformEmailCampaignStatus.Scheduled)
        {
            return BadRequest("Only draft/scheduled campaigns can be queued.");
        }

        var templateExists = await dbContext.PlatformEmailTemplates.AsNoTracking()
            .AnyAsync(x => x.Key == campaign.TemplateKey && x.IsActive, cancellationToken);
        if (!templateExists)
        {
            return BadRequest("Campaign template is not active.");
        }

        var tenantIds = DeserializeGuidList(campaign.TenantIdsJson);
        var audience = await ResolveAudienceAsync(
            campaign.SendToAllActiveTenants,
            tenantIds,
            campaign.SendToAllTenantUsers,
            cancellationToken);

        if (audience.Recipients.Count == 0)
        {
            return BadRequest("No recipients found for selected audience.");
        }

        var existingRecipients = await dbContext.PlatformEmailCampaignRecipients
            .Where(x => x.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);
        if (existingRecipients.Count > 0)
        {
            dbContext.PlatformEmailCampaignRecipients.RemoveRange(existingRecipients);
        }

        var now = DateTime.UtcNow;
        var recipients = audience.Recipients.Select(x => new PlatformEmailCampaignRecipient
        {
            CampaignId = campaign.Id,
            TenantAccountId = x.TenantId,
            TenantCode = x.TenantCode,
            TenantName = x.TenantName,
            RecipientEmail = x.RecipientEmail,
            Status = PlatformEmailRecipientStatus.Pending
        }).ToList();

        dbContext.PlatformEmailCampaignRecipients.AddRange(recipients);

        campaign.TotalRecipients = recipients.Count;
        campaign.SentCount = 0;
        campaign.FailedCount = 0;
        campaign.SkippedCount = 0;
        campaign.LastError = null;
        campaign.QueuedAtUtc = now;
        campaign.StartedAtUtc = null;
        campaign.CompletedAtUtc = null;
        campaign.CancelledAtUtc = null;
        campaign.Status = campaign.ScheduledAtUtc.HasValue && campaign.ScheduledAtUtc > now
            ? PlatformEmailCampaignStatus.Scheduled
            : PlatformEmailCampaignStatus.Queued;
        campaign.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AdminEmailCampaignQueueResultDto(
            campaign.Id,
            campaign.Status,
            campaign.TotalRecipients,
            campaign.ScheduledAtUtc));
    }

    [HttpPost("email/campaigns/{campaignId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelEmailCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.PlatformEmailCampaigns
            .FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status is PlatformEmailCampaignStatus.Completed or PlatformEmailCampaignStatus.CompletedWithErrors or PlatformEmailCampaignStatus.Cancelled)
        {
            return BadRequest("Campaign is already finalized.");
        }

        var now = DateTime.UtcNow;
        campaign.Status = PlatformEmailCampaignStatus.Cancelled;
        campaign.CancelledAtUtc = now;
        campaign.UpdatedAtUtc = now;

        var pendingRecipients = await dbContext.PlatformEmailCampaignRecipients
            .Where(x => x.CampaignId == campaignId && x.Status == PlatformEmailRecipientStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var recipient in pendingRecipients)
        {
            recipient.Status = PlatformEmailRecipientStatus.Cancelled;
            recipient.NextAttemptAtUtc = null;
            recipient.ProviderMessage = "Campaign cancelled by platform admin.";
            recipient.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("email/templates/{key}")]
    [ProducesResponseType(typeof(AdminEmailTemplateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminEmailTemplateDto>> GetEmailTemplateByKey(string key, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeTemplateKey(key);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return BadRequest("Template key is required.");
        }

        var template = await dbContext.PlatformEmailTemplates
            .AsNoTracking()
            .Where(x => x.Key == normalizedKey)
            .Select(x => new AdminEmailTemplateDto(
                x.Id,
                x.Key,
                x.Name,
                x.SubjectTemplate,
                x.BodyTemplate,
                x.Description,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return NotFound();
        }

        return Ok(template);
    }

    [HttpPut("email/templates/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertEmailTemplate(
        string key,
        [FromBody] UpsertAdminEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeTemplateKey(key);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return BadRequest("Template key is required.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        var subject = (request.SubjectTemplate ?? string.Empty).Trim();
        var body = (request.BodyTemplate ?? string.Empty).Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
        {
            return BadRequest("Name, subject and body are required.");
        }

        if (name.Length > 100 || subject.Length > 300 || body.Length > 8000 || (description?.Length ?? 0) > 500)
        {
            return BadRequest("Template fields exceed allowed length.");
        }

        var existing = await dbContext.PlatformEmailTemplates.FirstOrDefaultAsync(x => x.Key == normalizedKey, cancellationToken);
        if (existing is null)
        {
            dbContext.PlatformEmailTemplates.Add(new PlatformEmailTemplate
            {
                Key = normalizedKey,
                Name = name,
                SubjectTemplate = subject,
                BodyTemplate = body,
                Description = description,
                IsActive = request.IsActive
            });
        }
        else
        {
            existing.Name = name;
            existing.SubjectTemplate = subject;
            existing.BodyTemplate = body;
            existing.Description = description;
            existing.IsActive = request.IsActive;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("email/send")]
    [ProducesResponseType(typeof(AdminTenantEmailDispatchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminTenantEmailDispatchResultDto>> SendTenantEmail(
        [FromBody] SendTenantEmailRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateKey = NormalizeTemplateKey(request.TemplateKey);
        if (string.IsNullOrWhiteSpace(normalizedTemplateKey))
        {
            return BadRequest("TemplateKey is required.");
        }

        var template = await dbContext.PlatformEmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == normalizedTemplateKey, cancellationToken);

        if (template is null)
        {
            return NotFound($"Template '{normalizedTemplateKey}' was not found.");
        }

        if (!template.IsActive)
        {
            return BadRequest("Selected template is not active.");
        }

        var requestedTenantIds = request.SendToAllActiveTenants
            ? await dbContext.TenantAccounts.AsNoTracking()
                .Where(x => x.SubscriptionStatus == SubscriptionStatus.Active)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken)
            : (request.TenantIds ?? []).Distinct().ToList();

        if (requestedTenantIds.Count == 0)
        {
            return BadRequest("At least one tenant must be selected.");
        }

        var tenants = await dbContext.TenantAccounts.AsNoTracking()
            .Where(x => requestedTenantIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var tenantUserRows = await dbContext.Users.AsNoTracking()
            .Where(x => x.TenantAccountId.HasValue && requestedTenantIds.Contains(x.TenantAccountId.Value))
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new { x.TenantAccountId, x.Email })
            .ToListAsync(cancellationToken);

        var tenantUserLookup = tenantUserRows
            .Where(x => x.TenantAccountId.HasValue && !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.TenantAccountId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(u => u.Email.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

        var logs = new List<PlatformEmailDispatchLog>();
        var items = new List<AdminTenantEmailDispatchItemDto>();
        var now = DateTime.UtcNow;
        var triggeredByUserId = GetCurrentUserId(User);
        var triggeredByUserName = User.Identity?.Name;

        var sentCount = 0;
        var failedCount = 0;
        var skippedCount = 0;

        foreach (var tenant in tenants)
        {
            tenantUserLookup.TryGetValue(tenant.Id, out var tenantEmails);
            tenantEmails ??= [];

            var recipients = request.SendToAllTenantUsers
                ? tenantEmails
                : tenantEmails.Take(1).ToList();

            if (recipients.Count == 0)
            {
                skippedCount++;
                var message = "No recipient email found for tenant users.";

                logs.Add(new PlatformEmailDispatchLog
                {
                    TenantAccountId = tenant.Id,
                    TenantCode = tenant.Code,
                    TenantName = tenant.Name,
                    TemplateKey = normalizedTemplateKey,
                    RecipientEmail = string.Empty,
                    Subject = request.SubjectOverride ?? template.SubjectTemplate,
                    Body = request.BodyOverride ?? template.BodyTemplate,
                    Status = "Skipped",
                    ProviderMessage = message,
                    AttemptedAtUtc = now,
                    TriggeredByUserId = triggeredByUserId,
                    TriggeredByUserName = triggeredByUserName
                });

                items.Add(new AdminTenantEmailDispatchItemDto(
                    tenant.Id,
                    tenant.Code,
                    tenant.Name,
                    string.Empty,
                    "Skipped",
                    message));

                continue;
            }

            foreach (var recipient in recipients)
            {
                var templateVariables = BuildTemplateVariables(tenant, request.Variables);
                var subject = RenderTemplate((request.SubjectOverride ?? template.SubjectTemplate).Trim(), templateVariables);
                var body = RenderTemplate((request.BodyOverride ?? template.BodyTemplate).Trim(), templateVariables);

                var sendResult = await emailSender.SendAsync(
                    new EmailMessage(recipient, subject, body, IsHtml: true),
                    cancellationToken);

                var status = sendResult.IsSuccess ? "Sent" : sendResult.IsSkipped ? "Skipped" : "Failed";
                if (sendResult.IsSuccess) sentCount++;
                else if (sendResult.IsSkipped) skippedCount++;
                else failedCount++;

                logs.Add(new PlatformEmailDispatchLog
                {
                    TenantAccountId = tenant.Id,
                    TenantCode = tenant.Code,
                    TenantName = tenant.Name,
                    TemplateKey = normalizedTemplateKey,
                    RecipientEmail = recipient,
                    Subject = subject,
                    Body = body,
                    Status = status,
                    ProviderMessage = sendResult.Message,
                    AttemptedAtUtc = now,
                    SentAtUtc = sendResult.IsSuccess ? now : null,
                    TriggeredByUserId = triggeredByUserId,
                    TriggeredByUserName = triggeredByUserName
                });

                items.Add(new AdminTenantEmailDispatchItemDto(
                    tenant.Id,
                    tenant.Code,
                    tenant.Name,
                    recipient,
                    status,
                    sendResult.Message));
            }
        }

        if (logs.Count > 0)
        {
            dbContext.PlatformEmailDispatchLogs.AddRange(logs);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new AdminTenantEmailDispatchResultDto(
            requestedTenantIds.Count,
            tenants.Count,
            sentCount,
            failedCount,
            skippedCount,
            items));
    }

    [HttpGet("email/logs")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminEmailDispatchLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminEmailDispatchLogDto>>> GetEmailLogs(
        [FromQuery] string? q,
        [FromQuery] Guid? campaignId,
        [FromQuery] Guid? tenantId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PlatformEmailDispatchLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.RecipientEmail.ToLower().Contains(term) ||
                (x.TenantName != null && x.TenantName.ToLower().Contains(term)) ||
                x.TemplateKey.ToLower().Contains(term));
        }

        if (tenantId.HasValue)
        {
            query = query.Where(x => x.TenantAccountId == tenantId.Value);
        }

        if (campaignId.HasValue)
        {
            query = query.Where(x => x.CampaignId == campaignId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.ToLower() == normalizedStatus);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.AttemptedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.AttemptedAtUtc <= toUtc.Value);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 500);

        var logs = await query
            .OrderByDescending(x => x.AttemptedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new AdminEmailDispatchLogDto(
                x.Id,
                x.CampaignId,
                x.TenantAccountId,
                x.TenantCode,
                x.TenantName,
                x.TemplateKey,
                x.RecipientEmail,
                x.Subject,
                x.Status,
                x.ProviderMessage,
                x.AttemptedAtUtc,
                x.SentAtUtc,
                x.TriggeredByUserId,
                x.TriggeredByUserName))
            .ToListAsync(cancellationToken);

        return Ok(logs);
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

    private static string NormalizeTemplateKey(string? key)
    {
        return (key ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static Dictionary<string, string> BuildTemplateVariables(TenantAccount tenant, Dictionary<string, string>? customVariables)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TenantName"] = tenant.Name,
            ["TenantCode"] = tenant.Code,
            ["Plan"] = tenant.Plan.ToString(),
            ["SubscriptionStatus"] = tenant.SubscriptionStatus.ToString(),
            ["SubscriptionEndDate"] = tenant.SubscriptionEndAtUtc?.ToString("yyyy-MM-dd") ?? string.Empty,
            ["NowUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        foreach (var item in customVariables ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            variables[item.Key.Trim()] = item.Value ?? string.Empty;
        }

        return variables;
    }

    private async Task<(List<Guid> TenantIds, List<EmailAudienceRecipientRow> Recipients)> ResolveAudienceAsync(
        bool sendToAllActiveTenants,
        List<Guid>? tenantIds,
        bool sendToAllTenantUsers,
        CancellationToken cancellationToken)
    {
        var requestedTenantIds = sendToAllActiveTenants
            ? await dbContext.TenantAccounts.AsNoTracking()
                .Where(x => x.SubscriptionStatus == SubscriptionStatus.Active)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken)
            : (tenantIds ?? []).Distinct().ToList();

        if (requestedTenantIds.Count == 0)
        {
            return ([], []);
        }

        var tenants = await dbContext.TenantAccounts.AsNoTracking()
            .Where(x => requestedTenantIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (tenants.Count == 0)
        {
            return ([], []);
        }

        var validTenantIds = tenants.Select(x => x.Id).ToList();

        var tenantUserRows = await dbContext.Users.AsNoTracking()
            .Where(x =>
                x.TenantAccountId.HasValue &&
                validTenantIds.Contains(x.TenantAccountId.Value) &&
                !string.IsNullOrWhiteSpace(x.Email))
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                TenantId = x.TenantAccountId!.Value,
                Email = x.Email
            })
            .ToListAsync(cancellationToken);

        var userLookup = tenantUserRows
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                x => x.Key,
                x => x
                    .Select(v => NormalizeEmail(v.Email))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList());

        var recipients = new List<EmailAudienceRecipientRow>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tenant in tenants)
        {
            if (!userLookup.TryGetValue(tenant.Id, out var emails) || emails.Count == 0)
            {
                continue;
            }

            var selected = sendToAllTenantUsers ? emails : emails.Take(1).ToList();
            foreach (var email in selected)
            {
                var key = $"{tenant.Id:N}|{email}";
                if (!dedupe.Add(key))
                {
                    continue;
                }

                recipients.Add(new EmailAudienceRecipientRow(
                    tenant.Id,
                    tenant.Code,
                    tenant.Name,
                    email));
            }
        }

        return (validTenantIds, recipients);
    }

    private static AdminEmailCampaignDto MapCampaign(PlatformEmailCampaign campaign)
        => new(
            campaign.Id,
            campaign.Name,
            campaign.Description,
            campaign.TemplateKey,
            campaign.SubjectTemplate,
            campaign.BodyTemplate,
            campaign.IsHtml,
            campaign.SendToAllActiveTenants,
            campaign.SendToAllTenantUsers,
            DeserializeGuidList(campaign.TenantIdsJson),
            DeserializeVariables(campaign.VariablesJson),
            campaign.Status,
            campaign.ScheduledAtUtc,
            campaign.QueuedAtUtc,
            campaign.StartedAtUtc,
            campaign.CompletedAtUtc,
            campaign.CancelledAtUtc,
            campaign.TotalRecipients,
            campaign.SentCount,
            campaign.FailedCount,
            campaign.SkippedCount,
            campaign.LastError,
            campaign.CreatedAtUtc,
            campaign.UpdatedAtUtc);

    private static string SerializeGuidList(List<Guid>? ids)
    {
        var payload = (ids ?? []).Distinct().ToList();
        return JsonSerializer.Serialize(payload);
    }

    private static List<Guid> DeserializeGuidList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(raw) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeVariables(Dictionary<string, string>? variables)
    {
        var payload = (variables ?? new Dictionary<string, string>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToDictionary(x => x.Key.Trim(), x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(payload);
    }

    private static Dictionary<string, string> DeserializeVariables(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            return parsed is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        var value = template ?? string.Empty;
        return Regex.Replace(value, @"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", match =>
        {
            var token = match.Groups[1].Value;
            return variables.TryGetValue(token, out var resolved) ? resolved : match.Value;
        });
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

    private sealed record EmailAudienceRecipientRow(
        Guid? TenantId,
        string? TenantCode,
        string? TenantName,
        string RecipientEmail);
}






