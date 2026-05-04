using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendations;

public sealed record GetPurchaseRecommendationsQuery(
    Guid WarehouseId,
    Guid? SupplierCariAccountId,
    int AnalysisDays = 30,
    int CoverageDays = 21,
    int MaxItems = 30,
    bool CriticalOnly = false,
    string? RequestedByUserName = null) : IRequest<PurchaseRecommendationDto>;
