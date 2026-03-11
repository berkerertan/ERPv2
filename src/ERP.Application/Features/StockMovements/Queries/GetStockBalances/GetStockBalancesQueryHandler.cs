using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockBalances;

public sealed class GetStockBalancesQueryHandler(
    IStockMovementRepository stockMovementRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository)
    : IRequestHandler<GetStockBalancesQuery, IReadOnlyList<StockReportItemDto>>
{
    public async Task<IReadOnlyList<StockReportItemDto>> Handle(GetStockBalancesQuery request, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.GetAllAsync(cancellationToken);
        var warehouses = (await warehouseRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);
        var products = (await productRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);

        return movements
            .GroupBy(x => new { x.WarehouseId, x.ProductId })
            .Select(group =>
            {
                var balance = group.Sum(x => x.Type == StockMovementType.In ? x.Quantity : -x.Quantity);

                var product = products.TryGetValue(group.Key.ProductId, out var p)
                    ? p
                    : null;

                var warehouse = warehouses.TryGetValue(group.Key.WarehouseId, out var w)
                    ? w
                    : null;

                var unitPrice = product?.DefaultSalePrice ?? 0m;

                return new StockReportItemDto(
                    group.Key.ProductId,
                    product?.Name ?? "UNKNOWN",
                    product?.BarcodeEan13 ?? string.Empty,
                    warehouse?.Name ?? "UNKNOWN",
                    balance,
                    product?.Unit ?? "EA",
                    balance * unitPrice);
            })
            .OrderBy(x => x.WarehouseName)
            .ThenBy(x => x.ProductName)
            .ToList();
    }
}
