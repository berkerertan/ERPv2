using ERP.Domain.Enums;

namespace ERP.API.Contracts.Accounting;

public sealed record ChartOfAccountDto(
    Guid Id,
    string Code,
    string Name,
    AccountType Type,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record JournalEntryLineDto(
    Guid Id,
    Guid ChartOfAccountId,
    string ChartOfAccountCode,
    string ChartOfAccountName,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record JournalEntryDto(
    Guid Id,
    string VoucherNo,
    DateTime EntryDateUtc,
    JournalEntryStatus Status,
    string? Description,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<JournalEntryLineDto> Lines,
    DateTime CreatedAtUtc);

public sealed record CashAccountDto(
    Guid Id,
    string Code,
    string Name,
    string Currency,
    decimal Balance,
    DateTime CreatedAtUtc);

public sealed record CashTransactionDto(
    Guid Id,
    Guid CashAccountId,
    Guid? CariAccountId,
    Guid? FinanceMovementId,
    CashTransactionType Type,
    decimal Amount,
    DateTime TransactionDateUtc,
    string? Description,
    string? ReferenceNo,
    DateTime CreatedAtUtc);

public sealed record BankAccountDto(
    Guid Id,
    string BankName,
    string BranchName,
    string Iban,
    string Currency,
    decimal Balance,
    DateTime CreatedAtUtc);

public sealed record BankTransactionDto(
    Guid Id,
    Guid BankAccountId,
    Guid? CariAccountId,
    Guid? FinanceMovementId,
    BankTransactionType Type,
    decimal Amount,
    DateTime TransactionDateUtc,
    string? Description,
    string? ReferenceNo,
    DateTime CreatedAtUtc);

public sealed record CollectionPaymentResultDto(
    Guid FinanceMovementId,
    Guid? CashTransactionId,
    Guid? BankTransactionId,
    decimal CariBalance,
    decimal TreasuryBalance);

public sealed record CashFlowForecastDto(
    DateOnly Date,
    decimal ExpectedIn,
    decimal ExpectedOut,
    decimal Net);

public sealed record DueListItemDto(
    Guid CariAccountId,
    string CariCode,
    string CariName,
    DateOnly DueDate,
    decimal OpenAmount,
    int OverdueDays);

public sealed record ProductProfitabilityDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public sealed record CustomerProfitabilityDto(
    Guid CariAccountId,
    string CariCode,
    string CariName,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public sealed record BranchProfitabilityDto(
    Guid BranchId,
    string BranchCode,
    string BranchName,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);
