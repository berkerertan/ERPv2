using ERP.Domain.Enums;

namespace ERP.API.Contracts.Accounting;

public sealed record CheckNoteDto(
    Guid Id,
    string Code,
    CheckNoteType Type,
    CheckNoteDirection Direction,
    CheckNoteStatus Status,
    Guid CariAccountId,
    string CariCode,
    string CariName,
    decimal Amount,
    string Currency,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string? BankName,
    string? BranchName,
    string? AccountNo,
    string? SerialNo,
    string? Description,
    string? LastActionNote,
    Guid? RelatedFinanceMovementId,
    DateTime? SettledAtUtc,
    DateTime CreatedAtUtc);

public sealed record CheckNoteDueListItemDto(
    Guid Id,
    string Code,
    CheckNoteType Type,
    CheckNoteDirection Direction,
    CheckNoteStatus Status,
    Guid CariAccountId,
    string CariCode,
    string CariName,
    decimal Amount,
    string Currency,
    DateOnly DueDate,
    int RemainingDays);

public sealed record SettleCheckNoteResultDto(
    Guid CheckNoteId,
    CheckNoteStatus Status,
    Guid FinanceMovementId,
    Guid? CashTransactionId,
    Guid? BankTransactionId,
    decimal CariBalance,
    decimal TreasuryBalance);
