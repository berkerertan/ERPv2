using ERP.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Infrastructure.Persistence;

public sealed class ErpDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("STOKNET_DB_PROVIDER") ?? "SqlServer";
        var connectionString = Environment.GetEnvironmentVariable("STOKNET_CONNECTION");
        var optionsBuilder = new DbContextOptionsBuilder<ErpDbContext>();

        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlite(connectionString ?? "Data Source=stoknet-dev.db");
        }
        else
        {
            optionsBuilder.UseSqlServer(
                connectionString
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=StokNetDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
        }

        return new ErpDbContext(optionsBuilder.Options, new CurrentTenantService(new HttpContextAccessor()));
    }
}
