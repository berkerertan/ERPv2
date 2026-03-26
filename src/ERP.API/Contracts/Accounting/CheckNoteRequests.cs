using ERP.Domain.Enums;

namespace ERP.API.Contracts.Accounting;

public sealed class UpsertCheckNoteRequest
{
    public string Code { get; init; } = string.Empty;
    public CheckNoteType Type { get; init; }
    public CheckNoteDirection Direction { get; init; }
    public Guid CariAccountId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime IssueDateUtc { get; init; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; init; }
    public string? BankName { get; init; }
    public string? BranchName { get; init; }
    public string? AccountNo { get; init; }
    public string? SerialNo { get; init; }
    public string? Description { get; init; }
}

public sealed class UpdateCheckNoteStatusRequest
{
    public CheckNoteStatus Status { get; init; }
    public string? Note { get; init; }
}

public sealed class SettleCheckNoteRequest
{
    public TreasuryChannel Channel { get; init; }
    public Guid TreasuryAccountId { get; init; }
    public DateTime? TransactionDateUtc { get; init; }
    public string? Description { get; init; }
    public string? ReferenceNo { get; init; }
}
