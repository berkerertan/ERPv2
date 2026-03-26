using ERP.Application.Abstractions.Imports;
using ERP.Application.Abstractions.Media;
using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Infrastructure.Communication;
using ERP.Infrastructure.Authentication;
using ERP.Infrastructure.Imports;
using ERP.Infrastructure.Media;
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
        services.AddHttpContextAccessor();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ERPv2Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddDbContext<ErpDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantAccountRepository, TenantAccountRepository>();
        services.AddScoped<ICariAccountRepository, CariAccountRepository>();
        services.AddScoped<ICariDebtItemRepository, CariDebtItemRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IFinanceMovementRepository, FinanceMovementRepository>();

        services.AddScoped<ICariAccountExcelReader, ClosedXmlCariAccountExcelReader>();
        services.AddScoped<ICariDebtItemExcelReader, ClosedXmlCariDebtItemExcelReader>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IMediaStorageService, CloudinaryMediaStorageService>();

        return services;
    }
}
