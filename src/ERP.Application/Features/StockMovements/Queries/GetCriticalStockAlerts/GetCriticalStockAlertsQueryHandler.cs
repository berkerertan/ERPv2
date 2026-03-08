using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetCriticalStockAlerts;

public sealed class GetCriticalStockAlertsQueryHandler(
    IStockMovementRepository stockMovementRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository)
    : IRequestHandler<GetCriticalStockAlertsQuery, IReadOnlyList<CriticalStockAlertDto>>
{
    public async Task<IReadOnlyList<CriticalStockAlertDto>> Handle(GetCriticalStockAlertsQuery request, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.GetAllAsync(cancellationToken);
        var warehouses = await warehouseRepository.GetAllAsync(cancellationToken);
        var products = await productRepository.GetAllAsync(cancellationToken);

        var warehouseMap = warehouses.ToDictionary(x => x.Id, x => x.Code);
        var productMap = products.ToDictionary(x => x.Id);

        var balances = movements
            .GroupBy(x => new { x.WarehouseId, x.ProductId })
            .Select(group => new
            {
                group.Key.WarehouseId,
                group.Key.ProductId,
                Quantity = group.Sum(x => x.Type == StockMovementType.In ? x.Quantity : -x.Quantity)
            });

        var alerts = balances
            .Where(x => productMap.TryGetValue(x.ProductId, out var product)
                && product.CriticalStockLevel > 0
                && x.Quantity <= product.CriticalStockLevel)
            .Where(x => !request.WarehouseId.HasValue || x.WarehouseId == request.WarehouseId.Value)
            .Select(x =>
            {
                var product = productMap[x.ProductId];
                var missing = product.CriticalStockLevel - x.Quantity;
                return new CriticalStockAlertDto(
                    x.WarehouseId,
                    warehouseMap.TryGetValue(x.WarehouseId, out var whCode) ? whCode : "UNKNOWN",
                    x.ProductId,
                    product.Code,
                    product.Name,
                    x.Quantity,
                    product.CriticalStockLevel,
                    missing > 0 ? missing : 0);
            })
            .OrderBy(x => x.WarehouseCode)
            .ThenBy(x => x.ProductCode)
            .ToList();

        return alerts;
    }
}
