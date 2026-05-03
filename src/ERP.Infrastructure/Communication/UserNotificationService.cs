using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Security;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Communication;

public sealed class UserNotificationService(
    ErpDbContext dbContext,
    ICurrentTenantService currentTenantService) : IUserNotificationService
{
    public async Task<IReadOnlyList<UserNotificationModel>> GetAsync(bool? isRead, CancellationToken cancellationToken = default)
    {
        if (!currentTenantService.HasTenant)
        {
            return [];
        }

        var query = dbContext.UserNotifications.AsNoTracking().AsQueryable();
        if (isRead.HasValue)
        {
            query = query.Where(x => x.IsRead == isRead.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<UserNotificationModel?> PublishAsync(
        string type,
        string title,
        string message,
        string? link = null,
        CancellationToken cancellationToken = default)
    {
        if (!currentTenantService.HasTenant)
        {
            return null;
        }

        var row = new UserNotification
        {
            Type = NormalizeType(type),
            Title = title.Trim(),
            Message = message.Trim(),
            Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
            IsRead = false
        };

        dbContext.UserNotifications.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task<bool> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!currentTenantService.HasTenant)
        {
            return false;
        }

        var row = await dbContext.UserNotifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return false;
        }

        if (!row.IsRead)
        {
            row.IsRead = true;
            row.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        if (!currentTenantService.HasTenant)
        {
            return 0;
        }

        var unread = await dbContext.UserNotifications
            .Where(x => !x.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return 0;
        }

        foreach (var item in unread)
        {
            item.IsRead = true;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!currentTenantService.HasTenant)
        {
            return false;
        }

        var row = await dbContext.UserNotifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return false;
        }

        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteReadAsync(CancellationToken cancellationToken = default)
    {
        if (!currentTenantService.HasTenant)
        {
            return 0;
        }

        var readItems = await dbContext.UserNotifications
            .Where(x => x.IsRead)
            .ToListAsync(cancellationToken);

        if (readItems.Count == 0)
        {
            return 0;
        }

        foreach (var item in readItems)
        {
            item.MarkAsDeleted();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return readItems.Count;
    }

    private static UserNotificationModel Map(UserNotification row)
        => new(
            row.Id,
            NormalizeType(row.Type),
            row.Title,
            row.Message,
            row.IsRead,
            row.Link,
            row.CreatedAtUtc);

    private static string NormalizeType(string? type)
    {
        var normalized = (type ?? "info").Trim().ToLowerInvariant();
        return normalized switch
        {
            "danger" => "error",
            "success" => "success",
            "warning" => "warning",
            "error" => "error",
            _ => "info"
        };
    }
}
