using ERP.Application.Abstractions.Persistence;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistory;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendations;
using MediatR;
using System.Text.Json;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistoryById;

public sealed class GetPurchaseRecommendationHistoryByIdQueryHandler(
    IPurchaseRecommendationSnapshotRepository snapshotRepository)
    : IRequestHandler<GetPurchaseRecommendationHistoryByIdQuery, PurchaseRecommendationHistoryDetailDto?>
{
    public async Task<PurchaseRecommendationHistoryDetailDto?> Handle(
        GetPurchaseRecommendationHistoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var snapshot = await snapshotRepository.GetByIdAsync(request.SnapshotId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var items = JsonSerializer.Deserialize<IReadOnlyList<PurchaseRecommendationItemDto>>(snapshot.ItemsJson) ?? [];
        var groups = JsonSerializer.Deserialize<IReadOnlyList<PurchaseRecommendationSupplierGroupDto>>(snapshot.SupplierGroupsJson) ?? [];

        return new PurchaseRecommendationHistoryDetailDto(
            snapshot.Id,
            snapshot.WarehouseId,
            snapshot.WarehouseName,
            snapshot.SupplierCariAccountId,
            snapshot.SupplierName,
            snapshot.AnalysisDays,
            snapshot.CoverageDays,
            snapshot.MaxItems,
            snapshot.CriticalOnly,
            snapshot.CreatedByUserName,
            snapshot.CreatedAtUtc,
            new PurchaseRecommendationSummaryDto(
                snapshot.TotalItems,
                snapshot.CriticalItems,
                snapshot.TotalRecommendedQuantity,
                snapshot.TotalEstimatedCost),
            groups,
            items);
    }
}
