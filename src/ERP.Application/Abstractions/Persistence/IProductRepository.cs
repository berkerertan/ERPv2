using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        string sortDir,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SuggestAsync(
        string search,
        int limit,
        CancellationToken cancellationToken = default);
}
