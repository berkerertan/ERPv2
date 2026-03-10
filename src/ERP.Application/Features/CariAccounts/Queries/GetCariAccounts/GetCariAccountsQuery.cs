using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;

public sealed record GetCariAccountsQuery(
    string? Search = null,
    CariType? Type = null,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,
    string SortDir = "asc") : IRequest<IReadOnlyList<CariAccountDto>>;
