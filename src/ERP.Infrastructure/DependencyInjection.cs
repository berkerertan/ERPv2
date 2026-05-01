using ERP.Application.Abstractions.Imports;
using ERP.Application.Abstractions.Media;
using ERP.Application.Abstractions.DocumentScanner;
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
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.Data.SqlClient;

namespace ERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<AccountEmailOptions>(configuration.GetSection(AccountEmailOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));

        var provider = (configuration["Database:Provider"] ?? "SqlServer").Trim();
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");
        var sqlitePath = configuration["Database:SqlitePath"];
        var defaultSqlServerConnection =
            "Server=(localdb)\\MSSQLLocalDB;Database=StokNetDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var sqliteConnectionString = BuildSqliteConnectionString(configuredConnectionString, sqlitePath);
            services.AddDbContext<ErpDbContext>(options =>
                options.UseSqlite(sqliteConnectionString));
        }
        else
        {
            var sqlServerConnectionString = ResolveSqlServerConnectionString(configuration, configuredConnectionString);
            var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"];
            var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(sqlServerConnectionString))
            {
                if (isDevelopment)
                {
                    sqlServerConnectionString = defaultSqlServerConnection;
                }
                else
                {
                    throw new InvalidOperationException(
                        "SQL Server connection string is missing. Set 'ConnectionStrings__DefaultConnection' app setting or 'DefaultConnection' in Azure Connection Strings.");
                }
            }

            var (effectiveConnectionString, useAzureSqlAccessToken) =
                NormalizeSqlServerConnectionForTokenAuth(sqlServerConnectionString);

            if (useAzureSqlAccessToken)
            {
                services.AddSingleton<AzureSqlAccessTokenInterceptor>();
            }

            services.AddDbContext<ErpDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(effectiveConnectionString);
                if (useAzureSqlAccessToken)
                {
                    options.AddInterceptors(serviceProvider.GetRequiredService<AzureSqlAccessTokenInterceptor>());
                }
            });
        }

        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantAccountRepository, TenantAccountRepository>();
        services.AddScoped<ICariAccountRepository, CariAccountRepository>();
        services.AddScoped<ICariDebtItemRepository, CariDebtItemRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IInventoryCountSessionRepository, InventoryCountSessionRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IFinanceMovementRepository, FinanceMovementRepository>();

        services.AddScoped<ICariAccountExcelReader, ClosedXmlCariAccountExcelReader>();
        services.AddScoped<ICariDebtItemExcelReader, ClosedXmlCariDebtItemExcelReader>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAccountEmailService, AccountEmailService>();
        services.AddScoped<IUserNotificationService, UserNotificationService>();
        services.AddScoped<IMediaStorageService, CloudinaryMediaStorageService>();
        services.AddHttpClient<IDocumentScannerService, GeminiDocumentScannerService>((serviceProvider, client) =>
        {
            var geminiOptions = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            var claudeOptions = serviceProvider.GetRequiredService<IOptions<ClaudeOptions>>().Value;
            var timeoutSeconds = Math.Clamp(
                Math.Max(geminiOptions.RequestTimeoutSeconds, claudeOptions.RequestTimeoutSeconds),
                5,
                180);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        return services;
    }

    private static string? ResolveSqlServerConnectionString(IConfiguration configuration, string? configuredConnectionString)
    {
        var candidates = new[]
        {
            configuration["ConnectionStrings__DefaultConnection"],
            configuration["SQLAZURECONNSTR_DefaultConnection"],
            configuration["SQLCONNSTR_DefaultConnection"],
            configuration["CUSTOMCONNSTR_DefaultConnection"],
            configuration["DefaultConnection"],
            configuration["STOKNET_CONNECTION"],
            configuration["ConnectionStrings:DefaultConnection"]
        };

        foreach (var candidate in candidates)
        {
            if (IsUsableConnectionString(candidate))
            {
                return candidate;
            }
        }

        if (IsUsableConnectionString(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        return null;
    }

    private static bool IsUsableConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !value.StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase);
    }

    private static (string ConnectionString, bool UseAzureSqlAccessToken) NormalizeSqlServerConnectionForTokenAuth(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (connectionString, false);
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var authentication = builder.Authentication;
            var requiresAzureToken =
                authentication == SqlAuthenticationMethod.ActiveDirectoryManagedIdentity ||
                authentication == SqlAuthenticationMethod.ActiveDirectoryMSI ||
                authentication == SqlAuthenticationMethod.ActiveDirectoryDefault;

            if (!requiresAzureToken)
            {
                return (connectionString, false);
            }

            builder.Authentication = SqlAuthenticationMethod.NotSpecified;
            return (builder.ConnectionString, true);
        }
        catch
        {
            return (connectionString, false);
        }
    }

    private static string BuildSqliteConnectionString(string? configuredConnectionString, string? sqlitePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return NormalizeSqliteConnectionString(configuredConnectionString);
        }

        return $"Data Source={ResolveSqlitePath(sqlitePath)}";
    }

    private static string NormalizeSqliteConnectionString(string configuredConnectionString)
    {
        const string marker = "Data Source=";
        var markerIndex = configuredConnectionString.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return configuredConnectionString;
        }

        var valueStart = markerIndex + marker.Length;
        var valueEnd = configuredConnectionString.IndexOf(';', valueStart);
        if (valueEnd < 0)
        {
            valueEnd = configuredConnectionString.Length;
        }

        var rawPath = configuredConnectionString.Substring(valueStart, valueEnd - valueStart).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(rawPath) || rawPath.StartsWith('|') || Path.IsPathRooted(rawPath))
        {
            return configuredConnectionString;
        }

        var absolutePath = ResolveSqlitePath(rawPath);
        var absoluteSegment = $"Data Source={absolutePath}";
        return configuredConnectionString.Remove(markerIndex, valueEnd - markerIndex).Insert(markerIndex, absoluteSegment);
    }

    private static string ResolveSqlitePath(string? sqlitePath)
    {
        var appBaseDirectory = ResolveExecutableDirectory();
        if (string.IsNullOrWhiteSpace(sqlitePath))
        {
            sqlitePath = Path.Combine("data", "stoknet-offline.db");
        }

        var targetPath = sqlitePath.Trim();
        if (!Path.IsPathRooted(targetPath))
        {
            targetPath = Path.Combine(appBaseDirectory, targetPath);
        }

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        return targetPath;
    }

    private static string ResolveExecutableDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var processDirectory = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrWhiteSpace(processDirectory))
            {
                return processDirectory;
            }
        }

        return AppContext.BaseDirectory;
    }
}
