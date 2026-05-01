using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class InventoryCountSessionRepository : EfRepository<InventoryCountSession>, IInventoryCountSessionRepository
{
    private readonly ErpDbContext _dbContext;

    public InventoryCountSessionRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryCountSession?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryCountSessions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryCountSession>> GetFilteredAsync(
        Guid? warehouseId,
        bool includeCompleted,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InventoryCountSessions.AsNoTracking().AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (!includeCompleted)
        {
            query = query.Where(x => x.Status == InventoryCountSessionStatus.Open);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddWithItemsAsync(
        InventoryCountSession session,
        IEnumerable<InventoryCountSessionItem> items,
        CancellationToken cancellationToken = default)
    {
        session.CreatedAtUtc = DateTime.UtcNow;
        session.Items = items.ToList();
        foreach (var item in session.Items)
        {
            item.CreatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.InventoryCountSessions.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
