using ERP.Domain.Enums;

namespace ERP.Application.Features.Reports.Queries.GetCariBalances;

public sealed record CariBalanceDto(
    Guid CariAccountId,
    string Name,
    CariType Type,
    decimal Balance,
    DateTime? LastTransaction);
