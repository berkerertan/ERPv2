using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Infrastructure.Authentication;
using ERP.Infrastructure.Persistence.InMemory;
using ERP.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<InMemoryDataStore>();

        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<ICariAccountRepository, CariAccountRepository>();
        services.AddSingleton<ICompanyRepository, CompanyRepository>();
        services.AddSingleton<IBranchRepository, BranchRepository>();
        services.AddSingleton<IWarehouseRepository, WarehouseRepository>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IStockMovementRepository, StockMovementRepository>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
