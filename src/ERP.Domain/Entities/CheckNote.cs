using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class CheckNote : TenantOwnedEntity
{
    public string Code { get; set; } = string.Empty;
    public CheckNoteType Type { get; set; }
    public CheckNoteDirection Direction { get; set; }
    public CheckNoteStatus Status { get; set; } = CheckNoteStatus.Portfolio;

    public Guid CariAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime IssueDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; set; }

    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? AccountNo { get; set; }
    public string? SerialNo { get; set; }
    public string? Description { get; set; }
    public string? LastActionNote { get; set; }

    public Guid? RelatedFinanceMovementId { get; set; }
    public DateTime? SettledAtUtc { get; set; }
}
