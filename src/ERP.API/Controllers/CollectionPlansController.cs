using ERP.API.Common;
using ERP.API.Contracts.CollectionPlans;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/collection-plans")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class CollectionPlansController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(CollectionPlanDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionPlanDashboardDto>> GetDashboard(
        [FromQuery] CollectionPlanStatus? status,
        [FromQuery] CollectionPlanPriority? priority,
        [FromQuery] bool onlyAssignedToMe = false,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;

        var accounts = await dbContext.CariAccounts
            .AsNoTracking()
            .Where(x => (x.Type == CariType.BuyerBch || x.Type == CariType.Both) && x.CurrentBalance > 0)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.CurrentBalance,
                x.RiskLimit,
                x.MaturityDays
            })
            .ToListAsync(cancellationToken);

        var accountIds = accounts.Select(x => x.Id).ToList();
        var debtItems = accountIds.Count == 0
            ? []
            : await dbContext.CariDebtItems
                .AsNoTracking()
                .Where(x => accountIds.Contains(x.CariAccountId) && x.RemainingBalance > 0)
                .Select(x => new { x.CariAccountId, x.TransactionDate, x.RemainingBalance })
                .ToListAsync(cancellationToken);

        var planEntries = await dbContext.CollectionPlanEntries
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.CariAccountId))
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var planLookup = planEntries
            .GroupBy(x => x.CariAccountId)
            .ToDictionary(x => x.Key, x => x.First());

        var items = new List<CollectionPlanItemDto>();
        foreach (var account in accounts)
        {
            var threshold = today.AddDays(-(account.MaturityDays > 0 ? account.MaturityDays : 0));
            var overdue = debtItems.Where(x => x.CariAccountId == account.Id && x.TransactionDate.Date <= threshold).ToList();
            var overdueAmount = decimal.Round(overdue.Sum(x => x.RemainingBalance), 2, MidpointRounding.AwayFromZero);
            var overdueDays = overdue.Count == 0 ? 0 : Math.Max(0, (today - overdue.Min(x => x.TransactionDate).Date).Days);
            var riskUsage = account.RiskLimit > 0
                ? decimal.Round(account.CurrentBalance / account.RiskLimit, 4, MidpointRounding.AwayFromZero)
                : account.CurrentBalance > 0 ? 1m : 0m;

            var suggestedPriority = GetSuggestedPriority(overdueAmount, overdueDays, riskUsage);
            var suggestedAction = GetSuggestedAction(overdueDays, riskUsage);

            planLookup.TryGetValue(account.Id, out var plan);
            var effectiveStatus = plan?.Status ?? CollectionPlanStatus.Open;
            var effectivePriority = plan?.Priority ?? suggestedPriority;

            var dto = new CollectionPlanItemDto(
                account.Id,
                account.Code,
                account.Name,
                account.CurrentBalance,
                account.RiskLimit,
                overdueAmount,
                overdueDays,
                riskUsage,
                suggestedPriority,
                suggestedAction,
                plan?.Id,
                plan?.Title ?? $"Tahsilat takibi - {account.Name}",
                effectivePriority,
                effectiveStatus,
                plan?.NextActionDateUtc,
                plan?.PromiseDateUtc,
                plan?.AssignedToUserName,
                plan?.Notes,
                plan?.LastContactAtUtc,
                plan?.LastContactNote);

            if (dto.OverdueAmount <= 0 && dto.PlanEntryId is null)
            {
                continue;
            }

            if (status.HasValue && dto.Status != status.Value)
            {
                continue;
            }

            if (priority.HasValue && dto.Priority != priority.Value)
            {
                continue;
            }

            if (onlyAssignedToMe && !string.Equals(dto.AssignedToUserName, currentUserName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            items.Add(dto);
        }

        items = items
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.OverdueAmount)
            .ThenByDescending(x => x.OverdueDays)
            .ToList();

        var summary = new CollectionPlanSummaryDto(
            items.Count,
            items.Count(x => x.PlanEntryId.HasValue),
            items.Count(x => x.Priority == CollectionPlanPriority.Critical),
            items.Sum(x => x.OverdueAmount),
            items.Where(x => x.PlanEntryId.HasValue).Sum(x => x.OverdueAmount));

        return Ok(new CollectionPlanDashboardDto(summary, items));
    }

    [HttpPost("upsert")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> Upsert([FromBody] UpsertCollectionPlanRequest request, CancellationToken cancellationToken)
    {
        var account = await dbContext.CariAccounts.FirstOrDefaultAsync(x => x.Id == request.CariAccountId, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        var metrics = await BuildMetricsAsync(account, cancellationToken);
        var entry = await dbContext.CollectionPlanEntries
            .Where(x => x.CariAccountId == request.CariAccountId && x.Status != CollectionPlanStatus.Collected && x.Status != CollectionPlanStatus.Cancelled)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            entry = new CollectionPlanEntry
            {
                CariAccountId = request.CariAccountId
            };
            dbContext.CollectionPlanEntries.Add(entry);
        }

        entry.Title = string.IsNullOrWhiteSpace(request.Title) ? $"Tahsilat takibi - {account.Name}" : request.Title.Trim();
        entry.OverdueAmount = metrics.OverdueAmount;
        entry.OverdueDays = metrics.OverdueDays;
        entry.RiskUsageRate = metrics.RiskUsageRate;
        entry.Priority = request.Priority;
        entry.Status = request.Status;
        entry.NextActionDateUtc = request.NextActionDateUtc;
        entry.PromiseDateUtc = request.PromiseDateUtc;
        entry.AssignedToUserName = string.IsNullOrWhiteSpace(request.AssignedToUserName) ? User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name : request.AssignedToUserName.Trim();
        entry.Notes = request.Notes?.Trim();
        entry.LastContactNote = request.LastContactNote?.Trim();
        if (!string.IsNullOrWhiteSpace(entry.LastContactNote))
        {
            entry.LastContactAtUtc = DateTime.UtcNow;
        }
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(entry.Id);
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCollectionPlanStatusRequest request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.CollectionPlanEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        entry.Status = request.Status;
        entry.PromiseDateUtc = request.PromiseDateUtc;
        entry.Notes = request.Notes?.Trim() ?? entry.Notes;
        entry.LastContactNote = request.LastContactNote?.Trim() ?? entry.LastContactNote;
        if (!string.IsNullOrWhiteSpace(request.LastContactNote))
        {
            entry.LastContactAtUtc = DateTime.UtcNow;
        }
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static CollectionPlanPriority GetSuggestedPriority(decimal overdueAmount, int overdueDays, decimal riskUsageRate)
    {
        if (overdueAmount >= 250000m || overdueDays >= 45 || riskUsageRate >= 1m)
        {
            return CollectionPlanPriority.Critical;
        }

        if (overdueAmount >= 100000m || overdueDays >= 25 || riskUsageRate >= 0.85m)
        {
            return CollectionPlanPriority.High;
        }

        if (overdueAmount > 0 || overdueDays >= 10 || riskUsageRate >= 0.6m)
        {
            return CollectionPlanPriority.Medium;
        }

        return CollectionPlanPriority.Low;
    }

    private static string GetSuggestedAction(int overdueDays, decimal riskUsageRate)
    {
        if (overdueDays >= 45 || riskUsageRate >= 1m)
        {
            return "Yoneticiye eskale et";
        }

        if (overdueDays >= 20)
        {
            return "Odeme sozu al";
        }

        return "Musteriyi ara";
    }

    private async Task<(decimal OverdueAmount, int OverdueDays, decimal RiskUsageRate)> BuildMetricsAsync(CariAccount account, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var threshold = today.AddDays(-(account.MaturityDays > 0 ? account.MaturityDays : 0));
        var overdueItems = await dbContext.CariDebtItems
            .AsNoTracking()
            .Where(x => x.CariAccountId == account.Id && x.RemainingBalance > 0 && x.TransactionDate.Date <= threshold)
            .Select(x => new { x.TransactionDate, x.RemainingBalance })
            .ToListAsync(cancellationToken);

        var overdueAmount = decimal.Round(overdueItems.Sum(x => x.RemainingBalance), 2, MidpointRounding.AwayFromZero);
        var overdueDays = overdueItems.Count == 0 ? 0 : Math.Max(0, (today - overdueItems.Min(x => x.TransactionDate).Date).Days);
        var riskUsage = account.RiskLimit > 0
            ? decimal.Round(account.CurrentBalance / account.RiskLimit, 4, MidpointRounding.AwayFromZero)
            : account.CurrentBalance > 0 ? 1m : 0m;

        return (overdueAmount, overdueDays, riskUsage);
    }
}
