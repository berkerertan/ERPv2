using ERP.Domain.Enums;

namespace ERP.API.Contracts.CariAccounts;

public sealed record CreateCariAccountRequest(
    string Code,
    string Name,
    CariType Type,
    decimal RiskLimit,
    int MaturityDays,
    string? Phone = null);
