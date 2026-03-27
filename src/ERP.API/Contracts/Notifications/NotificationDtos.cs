namespace ERP.API.Contracts.Notifications;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    string? Link,
    DateTime CreatedAtUtc);

public record CreateNotificationRequest(
    string Type,
    string Title,
    string Message,
    string? Link,
    Guid? TargetUserId);
