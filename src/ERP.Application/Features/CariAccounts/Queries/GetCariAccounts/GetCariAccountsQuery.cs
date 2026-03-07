using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;

public sealed record GetCariAccountsQuery : IRequest<IReadOnlyList<CariAccountDto>>;
