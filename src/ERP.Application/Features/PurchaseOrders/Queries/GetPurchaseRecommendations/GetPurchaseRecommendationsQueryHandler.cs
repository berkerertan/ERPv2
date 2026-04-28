using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendations;

public sealed class GetPurchaseRecommendationsQueryHandler(
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    ICariAccountRepository cariAccountRepository,
    IPurchaseOrderRepository purchaseOrderRepository,
    ISalesOrderRepository salesOrderRepository,
    IStockMovementRepository stockMovementRepository)
    : IRequestHandler<GetPurchaseRecommendationsQuery, PurchaseRecommendationDto>
{
    public async Task<PurchaseRecommendationDto> Handle(
        GetPurchaseRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return CreateEmptyResult(request);
        }

        var analysisDays = Math.Clamp(request.AnalysisDays, 7, 120);
        var coverageDays = Math.Clamp(request.CoverageDays, 7, 90);
        var maxItems = Math.Clamp(request.MaxItems, 5, 100);
        var sinceUtc = DateTime.UtcNow.Date.AddDays(-analysisDays);

        var products = (await productRepository.GetAllAsync(cancellationToken))
            .Where(x => x.IsActive)
            .ToList();
        var suppliers = (await cariAccountRepository.GetAllAsync(cancellationToken))
            .Where(x => x.Type is CariType.Supplier or CariType.Both)
            .ToDictionary(x => x.Id);
        var salesOrders = await salesOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var purchaseOrders = await purchaseOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var stockMovements = await stockMovementRepository.GetAllAsync(cancellationToken);

        var onHandByProduct = stockMovements
            .Where(x => x.WarehouseId == request.WarehouseId)
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(x => x.Type == StockMovementType.In ? x.Quantity : -x.Quantity));

        var salesByProduct = salesOrders
            .Where(x =>
                x.Status == OrderStatus.Approved &&
                x.WarehouseId == request.WarehouseId &&
                x.OrderDateUtc >= sinceUtc)
            .SelectMany(x => x.Items)
            .GroupBy(x => x.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(x => x.Quantity));

        var incomingDraftByProduct = purchaseOrders
            .Where(x => x.Status == OrderStatus.Draft && x.WarehouseId == request.WarehouseId)
            .SelectMany(x => x.Items)
            .GroupBy(x => x.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(x => x.Quantity));

        var latestApprovedPurchaseByProduct = purchaseOrders
            .Where(x => x.Status == OrderStatus.Approved)
            .SelectMany(
                order => order.Items,
                (order, item) => new
                {
                    order.SupplierCariAccountId,
                    order.OrderDateUtc,
                    item.ProductId,
                    item.UnitPrice
                })
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(x => x.OrderDateUtc)
                    .First());

        var items = new List<PurchaseRecommendationItemDto>();

        foreach (var product in products)
        {
            var onHand = RoundQty(onHandByProduct.GetValueOrDefault(product.Id));
            var incomingDraft = RoundQty(incomingDraftByProduct.GetValueOrDefault(product.Id));
            var recentSalesQuantity = salesByProduct.GetValueOrDefault(product.Id);
            var averageDailySales = recentSalesQuantity <= 0
                ? 0m
                : RoundQty(recentSalesQuantity / analysisDays);
            var daysOfCover = averageDailySales <= 0
                ? 999m
                : Math.Round(onHand / averageDailySales, 1, MidpointRounding.AwayFromZero);

            var criticalLevel = Math.Max(0m, product.CriticalStockLevel);
            var minimumLevel = Math.Max(0m, product.MinimumStockLevel ?? 0m);
            var floorLevel = Math.Max(criticalLevel, minimumLevel);
            var dynamicTarget = Math.Max(floorLevel, averageDailySales * coverageDays);
            decimal? maximumLevel = product.MaximumStockLevel.HasValue && product.MaximumStockLevel.Value > 0m
                ? product.MaximumStockLevel.Value
                : null;
            var targetLevel = maximumLevel.HasValue
                ? Math.Max(floorLevel, Math.Min(dynamicTarget, maximumLevel.Value))
                : dynamicTarget;

            var availableQuantity = RoundQty(onHand + incomingDraft);
            var shortage = targetLevel - availableQuantity;
            if (shortage <= 0)
            {
                continue;
            }

            latestApprovedPurchaseByProduct.TryGetValue(product.Id, out var latestPurchase);
            var suggestedSupplierId = latestPurchase?.SupplierCariAccountId;
            var suggestedSupplierName = suggestedSupplierId.HasValue && suppliers.TryGetValue(suggestedSupplierId.Value, out var supplier)
                ? supplier.Name
                : null;

            if (request.SupplierCariAccountId.HasValue && suggestedSupplierId != request.SupplierCariAccountId)
            {
                continue;
            }

            var isCritical = availableQuantity <= floorLevel;
            if (request.CriticalOnly && !isCritical)
            {
                continue;
            }

            var recommendedQuantity = NormalizeRecommendedQuantity(product.Unit, shortage);
            var suggestedUnitPrice = Math.Max(0m, latestPurchase?.UnitPrice ?? product.LastPurchasePrice ?? 0m);
            var estimatedCost = Math.Round(recommendedQuantity * suggestedUnitPrice, 2, MidpointRounding.AwayFromZero);

            items.Add(new PurchaseRecommendationItemDto(
                product.Id,
                product.Code,
                product.Name,
                product.BarcodeEan13 ?? string.Empty,
                product.Unit,
                suggestedSupplierId,
                suggestedSupplierName,
                onHand,
                incomingDraft,
                availableQuantity,
                averageDailySales,
                daysOfCover,
                criticalLevel,
                minimumLevel,
                maximumLevel,
                RoundQty(targetLevel),
                recommendedQuantity,
                Math.Round(suggestedUnitPrice, 2, MidpointRounding.AwayFromZero),
                estimatedCost,
                isCritical,
                BuildReason(product, averageDailySales, daysOfCover, availableQuantity, targetLevel, incomingDraft, isCritical)));
        }

        var orderedItems = items
            .OrderByDescending(x => x.IsCritical)
            .ThenBy(x => x.DaysOfCover)
            .ThenByDescending(x => x.RecommendedOrderQuantity)
            .ThenBy(x => x.ProductName)
            .Take(maxItems)
            .ToList();

        var summary = new PurchaseRecommendationSummaryDto(
            orderedItems.Count,
            orderedItems.Count(x => x.IsCritical),
            orderedItems.Sum(x => x.RecommendedOrderQuantity),
            Math.Round(orderedItems.Sum(x => x.EstimatedCost), 2, MidpointRounding.AwayFromZero));

        return new PurchaseRecommendationDto(
            request.WarehouseId,
            request.SupplierCariAccountId,
            analysisDays,
            coverageDays,
            summary,
            orderedItems);
    }

    private static PurchaseRecommendationDto CreateEmptyResult(GetPurchaseRecommendationsQuery request)
    {
        return new PurchaseRecommendationDto(
            request.WarehouseId,
            request.SupplierCariAccountId,
            request.AnalysisDays,
            request.CoverageDays,
            new PurchaseRecommendationSummaryDto(0, 0, 0m, 0m),
            []);
    }

    private static decimal RoundQty(decimal value)
        => Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private static decimal NormalizeRecommendedQuantity(string? unit, decimal shortage)
    {
        var normalizedUnit = (unit ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedUnit is "EA" or "ADET" or "PCS" or "BOX" or "PK" or "PACK")
        {
            return Math.Max(1m, Math.Ceiling(shortage));
        }

        return Math.Max(0m, Math.Round(shortage, 2, MidpointRounding.AwayFromZero));
    }

    private static string BuildReason(
        Product product,
        decimal averageDailySales,
        decimal daysOfCover,
        decimal availableQuantity,
        decimal targetLevel,
        decimal incomingDraft,
        bool isCritical)
    {
        var parts = new List<string>();
        if (isCritical)
        {
            parts.Add("Kritik/Minimum seviyenin altinda");
        }

        if (averageDailySales > 0)
        {
            parts.Add($"Gunluk ort. tuketim {averageDailySales:0.###}");
            parts.Add($"Tahmini kapsama {daysOfCover:0.#} gun");
        }

        if (incomingDraft > 0)
        {
            parts.Add($"Taslak siparis yolda: {incomingDraft:0.###}");
        }

        parts.Add($"Hedef seviye {targetLevel:0.###}");
        parts.Add($"Mevcut kullanilabilir {availableQuantity:0.###}");

        if (parts.Count == 0)
        {
            parts.Add($"Stok seviyesi {product.Name} icin onerilen hedefin altinda");
        }

        return string.Join(" | ", parts);
    }
}
