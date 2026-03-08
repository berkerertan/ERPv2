using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class PurchaseOrderRepository : EfRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    private readonly ErpDbContext _dbContext;

    public PurchaseOrderRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PurchaseOrder?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseOrders.FirstOrDefaultAsync(
            x => x.OrderNo.ToLower() == orderNo.ToLower(),
            cancellationToken);
    }

    public async Task<PurchaseOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetAllWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.OrderDateUtc)
            .ToListAsync(cancellationToken);
    }
}
