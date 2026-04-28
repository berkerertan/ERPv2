namespace ERP.API.Contracts.CariAccounts;

public sealed record BuyerRiskSummaryItemDto(
    Guid CariAccountId,
    string CariAccountCode,
    string CariAccountName,
    decimal CurrentBalance,
    decimal RiskLimit,
    int MaturityDays,
    decimal OverdueAmount,
    int MaxOverdueDays,
    decimal RiskUsageRate,
    string Severity);

public sealed record BuyerRiskSummaryResponse(
    int TotalBuyerCount,
    int RiskyBuyerCount,
    int CriticalBuyerCount,
    decimal TotalCurrentBalance,
    decimal TotalOverdueAmount,
    IReadOnlyList<BuyerRiskSummaryItemDto> Items);
