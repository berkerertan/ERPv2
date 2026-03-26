using ERP.Application.Abstractions.Notifications;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ERP.API.Common;

public sealed class PlatformEmailCampaignProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptionsMonitor<EmailCampaignOptions> optionsMonitor,
    ILogger<PlatformEmailCampaignProcessor> logger) : BackgroundService
{
    private static readonly Regex TemplateRegex = new(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = optionsMonitor.CurrentValue;
            try
            {
                if (options.Enabled)
                {
                    await ProcessPendingCampaignsAsync(options, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Platform email campaign processor failed in loop.");
            }

            var delaySeconds = Math.Clamp(options.PollIntervalSeconds, 2, 300);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }

    private async Task ProcessPendingCampaignsAsync(EmailCampaignOptions options, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        var now = DateTime.UtcNow;
        var campaignIds = await dbContext.PlatformEmailCampaigns
            .AsNoTracking()
            .Where(x =>
                x.Status == PlatformEmailCampaignStatus.Queued ||
                x.Status == PlatformEmailCampaignStatus.Processing ||
                (x.Status == PlatformEmailCampaignStatus.Scheduled && x.ScheduledAtUtc.HasValue && x.ScheduledAtUtc <= now))
            .OrderBy(x => x.ScheduledAtUtc ?? x.CreatedAtUtc)
            .Select(x => x.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var campaignId in campaignIds)
        {
            await ProcessCampaignAsync(campaignId, options, cancellationToken);
        }
    }

    private async Task ProcessCampaignAsync(Guid campaignId, EmailCampaignOptions options, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = DateTime.UtcNow;
        var campaign = await dbContext.PlatformEmailCampaigns.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null || campaign.Status == PlatformEmailCampaignStatus.Cancelled)
        {
            return;
        }

        if (campaign.Status == PlatformEmailCampaignStatus.Scheduled && campaign.ScheduledAtUtc > now)
        {
            return;
        }

        if (campaign.Status is PlatformEmailCampaignStatus.Queued or PlatformEmailCampaignStatus.Scheduled)
        {
            campaign.Status = PlatformEmailCampaignStatus.Processing;
            campaign.QueuedAtUtc ??= now;
            campaign.StartedAtUtc ??= now;
            campaign.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var pendingRecipients = await dbContext.PlatformEmailCampaignRecipients
            .Where(x =>
                x.CampaignId == campaign.Id &&
                x.Status == PlatformEmailRecipientStatus.Pending &&
                (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(Math.Clamp(options.BatchSize, 10, 2000))
            .ToListAsync(cancellationToken);

        if (pendingRecipients.Count == 0)
        {
            var hasFutureRetry = await dbContext.PlatformEmailCampaignRecipients
                .AnyAsync(x => x.CampaignId == campaign.Id && x.Status == PlatformEmailRecipientStatus.Pending, cancellationToken);

            if (!hasFutureRetry)
            {
                await CompleteCampaignIfFinishedAsync(dbContext, campaign, cancellationToken);
            }

            return;
        }

        var tenantIds = pendingRecipients
            .Where(x => x.TenantAccountId.HasValue)
            .Select(x => x.TenantAccountId!.Value)
            .Distinct()
            .ToList();

        var tenantLookup = await dbContext.TenantAccounts
            .AsNoTracking()
            .Where(x => tenantIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        var customVariables = DeserializeVariables(campaign.VariablesJson);
        var logs = new List<PlatformEmailDispatchLog>(pendingRecipients.Count);
        var retryDelay = TimeSpan.FromMinutes(Math.Clamp(options.RetryDelayMinutes, 1, 24 * 60));
        var maxAttempts = Math.Clamp(options.MaxAttempts, 1, 20);

        foreach (var recipient in pendingRecipients)
        {
            var tenant = recipient.TenantAccountId.HasValue && tenantLookup.TryGetValue(recipient.TenantAccountId.Value, out var foundTenant)
                ? foundTenant
                : null;

            var variables = BuildVariables(tenant, recipient, customVariables);
            var subject = RenderTemplate(campaign.SubjectTemplate, variables);
            var body = RenderTemplate(campaign.BodyTemplate, variables);
            var attemptedAt = DateTime.UtcNow;

            var sendResult = await emailSender.SendAsync(
                new EmailMessage(recipient.RecipientEmail, subject, body, campaign.IsHtml),
                cancellationToken);

            recipient.AttemptCount += 1;
            recipient.LastAttemptedAtUtc = attemptedAt;
            recipient.ProviderMessage = sendResult.Message;

            var dispatchStatus = sendResult.IsSuccess
                ? "Sent"
                : sendResult.IsSkipped
                    ? "Skipped"
                    : "Failed";

            if (sendResult.IsSuccess)
            {
                recipient.Status = PlatformEmailRecipientStatus.Sent;
                recipient.SentAtUtc = attemptedAt;
                recipient.NextAttemptAtUtc = null;
                campaign.SentCount += 1;
            }
            else if (sendResult.IsSkipped)
            {
                recipient.Status = PlatformEmailRecipientStatus.Skipped;
                recipient.NextAttemptAtUtc = null;
                campaign.SkippedCount += 1;
            }
            else
            {
                if (recipient.AttemptCount >= maxAttempts)
                {
                    recipient.Status = PlatformEmailRecipientStatus.Failed;
                    recipient.NextAttemptAtUtc = null;
                    campaign.FailedCount += 1;
                }
                else
                {
                    recipient.Status = PlatformEmailRecipientStatus.Pending;
                    recipient.NextAttemptAtUtc = attemptedAt.Add(retryDelay);
                }
            }

            logs.Add(new PlatformEmailDispatchLog
            {
                CampaignId = campaign.Id,
                TenantAccountId = recipient.TenantAccountId,
                TenantCode = recipient.TenantCode,
                TenantName = recipient.TenantName,
                TemplateKey = campaign.TemplateKey,
                RecipientEmail = recipient.RecipientEmail,
                Subject = subject,
                Body = body,
                Status = dispatchStatus,
                ProviderMessage = sendResult.Message,
                AttemptedAtUtc = attemptedAt,
                SentAtUtc = sendResult.IsSuccess ? attemptedAt : null,
                TriggeredByUserId = campaign.CreatedByUserId,
                TriggeredByUserName = campaign.CreatedByUserName
            });
        }

        if (logs.Count > 0)
        {
            dbContext.PlatformEmailDispatchLogs.AddRange(logs);
        }

        campaign.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await CompleteCampaignIfFinishedAsync(dbContext, campaign, cancellationToken);
    }

    private static async Task CompleteCampaignIfFinishedAsync(
        ErpDbContext dbContext,
        PlatformEmailCampaign campaign,
        CancellationToken cancellationToken)
    {
        var hasPending = await dbContext.PlatformEmailCampaignRecipients
            .AnyAsync(x => x.CampaignId == campaign.Id && x.Status == PlatformEmailRecipientStatus.Pending, cancellationToken);

        if (hasPending)
        {
            return;
        }

        campaign.Status = campaign.FailedCount > 0
            ? PlatformEmailCampaignStatus.CompletedWithErrors
            : PlatformEmailCampaignStatus.Completed;
        campaign.CompletedAtUtc = DateTime.UtcNow;
        campaign.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static Dictionary<string, string> BuildVariables(
        TenantAccount? tenant,
        PlatformEmailCampaignRecipient recipient,
        Dictionary<string, string> customVariables)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TenantName"] = tenant?.Name ?? recipient.TenantName ?? string.Empty,
            ["TenantCode"] = tenant?.Code ?? recipient.TenantCode ?? string.Empty,
            ["Plan"] = tenant?.Plan.ToString() ?? string.Empty,
            ["SubscriptionStatus"] = tenant?.SubscriptionStatus.ToString() ?? string.Empty,
            ["SubscriptionEndDate"] = tenant?.SubscriptionEndAtUtc?.ToString("yyyy-MM-dd") ?? string.Empty,
            ["RecipientEmail"] = recipient.RecipientEmail,
            ["NowUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        foreach (var custom in customVariables)
        {
            if (string.IsNullOrWhiteSpace(custom.Key))
            {
                continue;
            }

            variables[custom.Key.Trim()] = custom.Value ?? string.Empty;
        }

        return variables;
    }

    private static string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        var value = template ?? string.Empty;
        return TemplateRegex.Replace(value, match =>
        {
            var token = match.Groups[1].Value;
            return variables.TryGetValue(token, out var resolved) ? resolved : match.Value;
        });
    }
}
