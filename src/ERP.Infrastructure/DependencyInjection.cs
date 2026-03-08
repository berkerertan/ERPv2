using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Infrastructure.Authentication;
using ERP.Infrastructure.Persistence;
using ERP.Infrastructure.Persistence.Repositories;
using ERP.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ERPv2Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        services.AddDbContext<ErpDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICariAccountRepository, CariAccountRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IFinanceMovementRepository, FinanceMovementRepository>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
