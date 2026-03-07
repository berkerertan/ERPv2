using ERP.Domain.Enums;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;

public sealed record CariAccountDto(
    Guid Id,
    string Code,
    string Name,
    CariType Type,
    decimal RiskLimit,
    int MaturityDays,
    decimal CurrentBalance);
