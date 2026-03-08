using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetCariBalances;

public sealed record GetCariBalancesQuery : IRequest<IReadOnlyList<CariBalanceDto>>;
