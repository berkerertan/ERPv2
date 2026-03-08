using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface ICariDebtItemRepository : IRepository<CariDebtItem>
{
    Task<IReadOnlyList<CariDebtItem>> GetByCariAccountIdAsync(Guid cariAccountId, CancellationToken cancellationToken = default);
}
