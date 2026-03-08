using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class CariDebtItemRepository : EfRepository<CariDebtItem>, ICariDebtItemRepository
{
    private readonly ErpDbContext _dbContext;

    public CariDebtItemRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CariDebtItem>> GetByCariAccountIdAsync(Guid cariAccountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CariDebtItems
            .AsNoTracking()
            .Where(x => x.CariAccountId == cariAccountId)
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
