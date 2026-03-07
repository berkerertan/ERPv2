using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockBalances;

public sealed class GetStockBalancesQueryHandler(
    IStockMovementRepository stockMovementRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository)
    : IRequestHandler<GetStockBalancesQuery, IReadOnlyList<StockBalanceDto>>
{
    public async Task<IReadOnlyList<StockBalanceDto>> Handle(GetStockBalancesQuery request, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.GetAllAsync(cancellationToken);
        var warehouses = (await warehouseRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.Code);
        var products = (await productRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.Code);

        return movements
            .GroupBy(x => new { x.WarehouseId, x.ProductId })
            .Select(group => new StockBalanceDto(
                group.Key.WarehouseId,
                warehouses.TryGetValue(group.Key.WarehouseId, out var warehouseCode) ? warehouseCode : "UNKNOWN",
                group.Key.ProductId,
                products.TryGetValue(group.Key.ProductId, out var productCode) ? productCode : "UNKNOWN",
                group.Sum(x => x.Type == StockMovementType.In ? x.Quantity : -x.Quantity)))
            .OrderBy(x => x.WarehouseCode)
            .ThenBy(x => x.ProductCode)
            .ToList();
    }
}
