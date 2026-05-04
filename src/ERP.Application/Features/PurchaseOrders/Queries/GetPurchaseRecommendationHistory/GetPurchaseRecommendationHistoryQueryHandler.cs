using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistory;

public sealed class GetPurchaseRecommendationHistoryQueryHandler(
    IPurchaseRecommendationSnapshotRepository snapshotRepository)
    : IRequestHandler<GetPurchaseRecommendationHistoryQuery, IReadOnlyList<PurchaseRecommendationHistoryListItemDto>>
{
    public async Task<IReadOnlyList<PurchaseRecommendationHistoryListItemDto>> Handle(
        GetPurchaseRecommendationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var snapshots = await snapshotRepository.GetRecentAsync(request.Take, cancellationToken);

        return snapshots
            .Select(x => new PurchaseRecommendationHistoryListItemDto(
                x.Id,
                x.WarehouseId,
                x.WarehouseName,
                x.SupplierCariAccountId,
                x.SupplierName,
                x.AnalysisDays,
                x.CoverageDays,
                x.MaxItems,
                x.CriticalOnly,
                x.TotalItems,
                x.CriticalItems,
                x.TotalRecommendedQuantity,
                x.TotalEstimatedCost,
                x.CreatedByUserName,
                x.CreatedAtUtc))
            .ToList();
    }
}
