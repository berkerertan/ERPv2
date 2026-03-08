using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class FinanceMovementRepository : EfRepository<FinanceMovement>, IFinanceMovementRepository
{
    public FinanceMovementRepository(ErpDbContext dbContext) : base(dbContext)
    {
    }
}
