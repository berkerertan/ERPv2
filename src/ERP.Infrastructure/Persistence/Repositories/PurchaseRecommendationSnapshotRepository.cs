using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class PurchaseRecommendationSnapshotRepository : EfRepository<PurchaseRecommendationSnapshot>, IPurchaseRecommendationSnapshotRepository
{
    private readonly ErpDbContext _dbContext;

    public PurchaseRecommendationSnapshotRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PurchaseRecommendationSnapshot>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 50);
        return await _dbContext.PurchaseRecommendationSnapshots
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(normalizedTake)
            .ToListAsync(cancellationToken);
    }
}
