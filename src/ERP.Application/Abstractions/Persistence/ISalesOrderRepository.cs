using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface ISalesOrderRepository : IRepository<SalesOrder>
{
    Task<SalesOrder?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);
    Task<SalesOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesOrder>> GetAllWithItemsAsync(CancellationToken cancellationToken = default);
}
