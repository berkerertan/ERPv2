using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockBalances;

public sealed record GetStockBalancesQuery : IRequest<IReadOnlyList<StockBalanceDto>>;
