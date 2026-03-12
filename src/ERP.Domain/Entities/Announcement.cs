using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Announcement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int Priority { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
