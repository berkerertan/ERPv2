using ERP.Application.Features.StockMovements.Queries.GetStockMovements;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockMovementById;

public sealed record GetStockMovementByIdQuery(Guid StockMovementId) : IRequest<StockMovementDto>;
