using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class JournalEntryLine : TenantOwnedEntity
{
    public Guid JournalEntryId { get; set; }
    public Guid ChartOfAccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

