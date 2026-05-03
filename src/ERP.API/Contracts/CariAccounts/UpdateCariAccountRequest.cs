using ERP.Domain.Enums;

namespace ERP.API.Contracts.CariAccounts;

public sealed record UpdateCariAccountRequest(
    string Code,
    string Name,
    CariType Type,
    decimal RiskLimit,
    int MaturityDays,
    int SupplierLeadTimeDays = 0,
    string? Phone = null);
