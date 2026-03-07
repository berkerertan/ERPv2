using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
