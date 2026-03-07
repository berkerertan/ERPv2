using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
