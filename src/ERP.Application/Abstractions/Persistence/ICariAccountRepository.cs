using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface ICariAccountRepository : IRepository<CariAccount>
{
    Task<CariAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
