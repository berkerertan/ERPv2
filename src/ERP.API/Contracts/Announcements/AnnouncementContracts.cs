namespace ERP.API.Contracts.Announcements;

public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Content,
    bool IsPublished,
    int Priority,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UpsertAnnouncementRequest(
    string Title,
    string Content,
    bool IsPublished,
    int Priority,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc);
