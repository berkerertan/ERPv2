using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class JournalEntry : TenantOwnedEntity
{
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime EntryDateUtc { get; set; } = DateTime.UtcNow;
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    public string? Description { get; set; }

    public ICollection<JournalEntryLine> Lines { get; set; } = [];
}

