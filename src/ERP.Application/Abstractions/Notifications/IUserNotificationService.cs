namespace ERP.Application.Abstractions.Notifications;

public sealed record UserNotificationModel(
    Guid Id,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    string? Link,
    DateTime CreatedAtUtc);

public interface IUserNotificationService
{
    Task<IReadOnlyList<UserNotificationModel>> GetAsync(bool? isRead, CancellationToken cancellationToken = default);
    Task<UserNotificationModel?> PublishAsync(
        string type,
        string title,
        string message,
        string? link = null,
        CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
