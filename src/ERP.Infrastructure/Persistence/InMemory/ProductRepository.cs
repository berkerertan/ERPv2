using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Common;
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
            return Task.FromResult(Entities.FirstOrDefault(
                x => !x.IsDeleted && x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
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
                    || (x.QrCode is not null && x.QrCode.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    || CsvListSerializer.ContainsToken(x.AlternativeBarcodesCsv, normalized))));
        }
    }

    public Task<IReadOnlyList<Product>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        string sortDir,
        CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var normalizedSearch = NormalizeText(search);
            var safePage = page <= 0 ? 1 : page;
            var safePageSize = pageSize <= 0 ? 50 : Math.Min(pageSize, 200);

            IEnumerable<Product> query = Entities.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(x =>
                    x.Code.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                    || x.Category.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                    || (x.ShortDescription != null && x.ShortDescription.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.SubCategory != null && x.SubCategory.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.Brand != null && x.Brand.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.ProductType != null && x.ProductType.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.BarcodeEan13 != null && x.BarcodeEan13.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.QrCode != null && x.QrCode.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.AlternativeBarcodesCsv != null && x.AlternativeBarcodesCsv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)));
            }

            query = ApplySort(query, sortBy, sortDir);

            return Task.FromResult<IReadOnlyList<Product>>(query
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToList());
        }
    }

    public Task<IReadOnlyList<Product>> SuggestAsync(
        string search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var normalizedSearch = NormalizeText(search);
            if (string.IsNullOrWhiteSpace(normalizedSearch))
            {
                return Task.FromResult<IReadOnlyList<Product>>([]);
            }

            var safeLimit = limit <= 0 ? 8 : Math.Min(limit, 20);

            var products = Entities
                .Where(x => !x.IsDeleted)
                .Where(x =>
                    x.Code.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                    || (x.ShortDescription != null && x.ShortDescription.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.Brand != null && x.Brand.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.SubCategory != null && x.SubCategory.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.BarcodeEan13 != null && x.BarcodeEan13.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.AlternativeBarcodesCsv != null && x.AlternativeBarcodesCsv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    || (x.QrCode != null && x.QrCode.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.Code.StartsWith(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.Name.StartsWith(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x.Code)
                .Take(safeLimit)
                .ToList();

            return Task.FromResult<IReadOnlyList<Product>>(products);
        }
    }

    private static IEnumerable<Product> ApplySort(IEnumerable<Product> query, string? sortBy, string sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var key = sortBy?.Trim().ToLowerInvariant();

        return key switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "category" => desc ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
            "brand" => desc ? query.OrderByDescending(x => x.Brand) : query.OrderBy(x => x.Brand),
            "type" or "producttype" => desc ? query.OrderByDescending(x => x.ProductType) : query.OrderBy(x => x.ProductType),
            "price" or "defaultsaleprice" => desc ? query.OrderByDescending(x => x.DefaultSalePrice) : query.OrderBy(x => x.DefaultSalePrice),
            _ => desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code)
        };
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
