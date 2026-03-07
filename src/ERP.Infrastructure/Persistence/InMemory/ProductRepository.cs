using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class ProductRepository : InMemoryRepository<Product>, IProductRepository
{
    private readonly InMemoryDataStore _store;

    public ProductRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<Product> Entities => _store.Products;

    public Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(Entities.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
