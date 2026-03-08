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
            return Task.FromResult(Entities.FirstOrDefault(x => !x.IsDeleted && x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalized = barcode.Trim();
        lock (_store.SyncRoot)
        {
            return Task.FromResult(Entities.FirstOrDefault(x => !x.IsDeleted
                && (x.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                    || (x.BarcodeEan13 is not null && x.BarcodeEan13.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    || (x.QrCode is not null && x.QrCode.Equals(normalized, StringComparison.OrdinalIgnoreCase)))));
        }
    }
}
