using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IPurchaseRecommendationSnapshotRepository : IRepository<PurchaseRecommendationSnapshot>
{
    Task<IReadOnlyList<PurchaseRecommendationSnapshot>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
