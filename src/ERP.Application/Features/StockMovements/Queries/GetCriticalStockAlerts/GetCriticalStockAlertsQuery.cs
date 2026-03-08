using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetCriticalStockAlerts;

public sealed record GetCriticalStockAlertsQuery(Guid? WarehouseId = null) : IRequest<IReadOnlyList<CriticalStockAlertDto>>;
