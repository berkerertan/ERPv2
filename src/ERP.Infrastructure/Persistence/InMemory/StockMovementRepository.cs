using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using ERP.Domain.Enums;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class StockMovementRepository : InMemoryRepository<StockMovement>, IStockMovementRepository
{
    private readonly InMemoryDataStore _store;

    public StockMovementRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<StockMovement> Entities => _store.StockMovements;

    public Task<decimal> GetCurrentQuantityAsync(Guid warehouseId, Guid productId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var balance = Entities
                .Where(x => x.WarehouseId == warehouseId && x.ProductId == productId)
                .Sum(x => x.Type == StockMovementType.In ? x.Quantity : -x.Quantity);

            return Task.FromResult(balance);
        }
    }
}
