using ERP.Domain.Enums;

namespace ERP.API.Contracts.Accounting;

public sealed class UpsertChartOfAccountRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public AccountType Type { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class UpsertJournalEntryRequest
{
    public string? VoucherNo { get; init; }
    public DateTime EntryDateUtc { get; init; } = DateTime.UtcNow;
    public string? Description { get; init; }
    public bool PostOnCreate { get; init; }
    public List<UpsertJournalEntryLineRequest> Lines { get; init; } = [];
}

public sealed class UpsertJournalEntryLineRequest
{
    public Guid ChartOfAccountId { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string? Description { get; init; }
}

public sealed class ReverseJournalEntryRequest
{
    public string? VoucherNo { get; init; }
    public string? Description { get; init; }
}

public sealed class UpsertCashAccountRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "TRY";
}

public sealed class CreateCashTransactionRequest
{
    public Guid? CariAccountId { get; init; }
    public Guid? FinanceMovementId { get; init; }
    public CashTransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public DateTime? TransactionDateUtc { get; init; }
    public string? Description { get; init; }
    public string? ReferenceNo { get; init; }
}

public sealed class UpsertBankAccountRequest
{
    public string BankName { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public string Iban { get; init; } = string.Empty;
    public string Currency { get; init; } = "TRY";
}

public sealed class CreateBankTransactionRequest
{
    public Guid? CariAccountId { get; init; }
    public Guid? FinanceMovementId { get; init; }
    public BankTransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public DateTime? TransactionDateUtc { get; init; }
    public string? Description { get; init; }
    public string? ReferenceNo { get; init; }
}

public sealed class CreateCollectionPaymentRequest
{
    public Guid CariAccountId { get; init; }
    public FinanceMovementType Type { get; init; }
    public decimal Amount { get; init; }
    public string? Description { get; init; }
    public string? ReferenceNo { get; init; }
    public TreasuryChannel Channel { get; init; }
    public Guid TreasuryAccountId { get; init; }
    public DateTime? TransactionDateUtc { get; init; }
}
