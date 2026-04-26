using ERP.Application.Abstractions.Security;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace ERP.API.Common;

public sealed class ActivityLoggingMiddleware(RequestDelegate next, ILogger<ActivityLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ErpDbContext dbContext, ICurrentTenantService currentTenantService)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (ShouldSkip(path))
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();

            Guid? userId = null;
            var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (Guid.TryParse(sub, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var log = new SystemActivityLog
            {
                TenantAccountId = currentTenantService.TenantId,
                UserId = userId,
                UserName = context.User.Identity?.Name,
                HttpMethod = context.Request.Method,
                Path = path,
                StatusCode = context.Response.StatusCode,
                DurationMs = (int)sw.ElapsedMilliseconds,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                OccurredAtUtc = DateTime.UtcNow
            };

            try
            {
                dbContext.SystemActivityLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Activity log persistence failed for {Method} {Path}. Request will continue without audit log.",
                    context.Request.Method,
                    path);
            }
        }
    }

    private static bool ShouldSkip(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/", StringComparison.OrdinalIgnoreCase) && path.Length == 1;
    }
}
