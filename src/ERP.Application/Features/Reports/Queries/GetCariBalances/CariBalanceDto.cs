using ERP.Domain.Enums;

namespace ERP.Application.Features.Reports.Queries.GetCariBalances;

public sealed record CariBalanceDto(
    Guid CariAccountId,
    string Code,
    string Name,
    CariType Type,
    decimal CurrentBalance,
    decimal RiskLimit);
