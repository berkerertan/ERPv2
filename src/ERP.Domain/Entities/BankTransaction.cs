using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class BankTransaction : TenantOwnedEntity
{
    public Guid BankAccountId { get; set; }
    public Guid? CariAccountId { get; set; }
    public Guid? FinanceMovementId { get; set; }
    public BankTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDateUtc { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string? ReferenceNo { get; set; }
}

