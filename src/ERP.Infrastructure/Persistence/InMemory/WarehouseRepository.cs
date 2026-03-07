using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class WarehouseRepository : InMemoryRepository<Warehouse>, IWarehouseRepository
{
    private readonly InMemoryDataStore _store;

    public WarehouseRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<Warehouse> Entities => _store.Warehouses;

    public Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(Entities.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
