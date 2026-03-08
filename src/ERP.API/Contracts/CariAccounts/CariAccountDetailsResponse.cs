using ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;
using ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;

namespace ERP.API.Contracts.CariAccounts;

public sealed record CariAccountDetailsResponse(
    CariAccountDto Account,
    IReadOnlyList<CariDebtItemDto> Items);
