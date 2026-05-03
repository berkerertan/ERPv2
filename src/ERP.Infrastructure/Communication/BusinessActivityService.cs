using ERP.Application.Abstractions.Auditing;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;

namespace ERP.Infrastructure.Communication;

public sealed class BusinessActivityService(ErpDbContext dbContext) : IBusinessActivityService
{
    public async Task LogAsync(BusinessActivityLogEntry entry, CancellationToken cancellationToken = default)
    {
        var log = new SystemActivityLog
        {
            TenantAccountId = entry.TenantAccountId,
            UserId = entry.UserId,
            UserName = Normalize(entry.UserName, 100),
            Description = Normalize(entry.Description, 500),
            HttpMethod = NormalizeAction(entry.Action),
            Path = Normalize(entry.Path, 500) ?? "/audit",
            StatusCode = entry.StatusCode,
            DurationMs = 0,
            OccurredAtUtc = DateTime.UtcNow
        };

        await dbContext.SystemActivityLogs.AddAsync(log, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeAction(string? value)
    {
        var action = Normalize(value, 10)?.ToUpperInvariant();
        return string.IsNullOrWhiteSpace(action) ? "AUDIT" : action;
    }

    private static string? Normalize(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
