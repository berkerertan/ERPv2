using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class InMemoryDataStore
{
    public object SyncRoot { get; } = new();
    public List<AppUser> Users { get; } = [];
    public List<CariAccount> CariAccounts { get; } = [];
    public List<Company> Companies { get; } = [];
    public List<Branch> Branches { get; } = [];
    public List<Warehouse> Warehouses { get; } = [];
    public List<Product> Products { get; } = [];
    public List<StockMovement> StockMovements { get; } = [];
}
