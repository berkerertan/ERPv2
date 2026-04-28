using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class StockMovementRepository : EfRepository<StockMovement>, IStockMovementRepository
{
    private readonly ErpDbContext _dbContext;

    public StockMovementRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetCurrentQuantityAsync(Guid warehouseId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockMovements
            .Where(x => x.WarehouseId == warehouseId && x.ProductId == productId)
            .SumAsync(x => x.Type == StockMovementType.In ? x.Quantity : -x.Quantity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<StockMovement> entities, CancellationToken cancellationToken = default)
    {
        var materialized = entities.ToList();
        if (materialized.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var entity in materialized)
        {
            entity.CreatedAtUtc = now;
        }

        await _dbContext.StockMovements.AddRangeAsync(materialized, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
