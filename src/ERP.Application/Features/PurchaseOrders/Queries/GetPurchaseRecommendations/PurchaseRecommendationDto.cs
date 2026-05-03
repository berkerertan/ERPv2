namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendations;

public sealed record PurchaseRecommendationSummaryDto(
    int TotalItems,
    int CriticalItems,
    decimal TotalRecommendedQuantity,
    decimal TotalEstimatedCost);

public sealed record PurchaseRecommendationSupplierGroupDto(
    Guid? SupplierCariAccountId,
    string SupplierName,
    int SupplierLeadTimeDays,
    int ItemCount,
    decimal TotalRecommendedQuantity,
    decimal TotalEstimatedCost);

public sealed record PurchaseRecommendationItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Barcode,
    string Unit,
    Guid? SuggestedSupplierCariAccountId,
    string? SuggestedSupplierName,
    decimal OnHandQuantity,
    decimal IncomingDraftQuantity,
    decimal AvailableQuantity,
    decimal AverageDailySales,
    decimal DaysOfCover,
    int SupplierLeadTimeDays,
    int PlanningDays,
    decimal CriticalStockLevel,
    decimal MinimumStockLevel,
    decimal? MaximumStockLevel,
    decimal TargetStockLevel,
    decimal RecommendedOrderQuantity,
    decimal SuggestedUnitPrice,
    decimal EstimatedCost,
    bool IsCritical,
    string RecommendationReason);

public sealed record PurchaseRecommendationDto(
    Guid WarehouseId,
    Guid? SupplierCariAccountId,
    int AnalysisDays,
    int CoverageDays,
    PurchaseRecommendationSummaryDto Summary,
    IReadOnlyList<PurchaseRecommendationSupplierGroupDto> SupplierGroups,
    IReadOnlyList<PurchaseRecommendationItemDto> Items);
