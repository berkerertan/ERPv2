using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistory;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistoryById;

public sealed record GetPurchaseRecommendationHistoryByIdQuery(Guid SnapshotId)
    : IRequest<PurchaseRecommendationHistoryDetailDto?>;
