using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class SalesOrderRepository : EfRepository<SalesOrder>, ISalesOrderRepository
{
    private readonly ErpDbContext _dbContext;

    public SalesOrderRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SalesOrder?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders.FirstOrDefaultAsync(
            x => x.OrderNo.ToLower() == orderNo.ToLower(),
            cancellationToken);
    }

    public async Task<SalesOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SalesOrder>> GetAllWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.OrderDateUtc)
            .ToListAsync(cancellationToken);
    }
}
