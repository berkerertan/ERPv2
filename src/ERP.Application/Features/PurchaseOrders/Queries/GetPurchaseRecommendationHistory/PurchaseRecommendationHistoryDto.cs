using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendations;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistory;

public sealed record PurchaseRecommendationHistoryListItemDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    Guid? SupplierCariAccountId,
    string? SupplierName,
    int AnalysisDays,
    int CoverageDays,
    int MaxItems,
    bool CriticalOnly,
    int TotalItems,
    int CriticalItems,
    decimal TotalRecommendedQuantity,
    decimal TotalEstimatedCost,
    string? CreatedByUserName,
    DateTime CreatedAtUtc);

public sealed record PurchaseRecommendationHistoryDetailDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    Guid? SupplierCariAccountId,
    string? SupplierName,
    int AnalysisDays,
    int CoverageDays,
    int MaxItems,
    bool CriticalOnly,
    string? CreatedByUserName,
    DateTime CreatedAtUtc,
    PurchaseRecommendationSummaryDto Summary,
    IReadOnlyList<PurchaseRecommendationSupplierGroupDto> SupplierGroups,
    IReadOnlyList<PurchaseRecommendationItemDto> Items);
