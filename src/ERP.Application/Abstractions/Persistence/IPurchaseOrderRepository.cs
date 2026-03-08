using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetAllWithItemsAsync(CancellationToken cancellationToken = default);
}
