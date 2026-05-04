using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistory;

public sealed record GetPurchaseRecommendationHistoryQuery(int Take = 12)
    : IRequest<IReadOnlyList<PurchaseRecommendationHistoryListItemDto>>;
