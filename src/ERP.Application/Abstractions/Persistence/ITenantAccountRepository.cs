using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface ITenantAccountRepository : IRepository<TenantAccount>
{
    Task<TenantAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
