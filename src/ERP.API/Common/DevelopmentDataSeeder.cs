using ERP.Application.Abstractions.Security;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Common;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ErpDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentDataSeeder");

        await dbContext.Database.MigrateAsync(cancellationToken);

        await EnsureDefaultPlanSettingsAsync(dbContext, cancellationToken);
        await EnsureDefaultLandingContentAsync(dbContext, cancellationToken);

        var primaryTenant = await EnsureTenantAsync(
            dbContext,
            name: "Dev Retail",
            code: "dev-retail",
            plan: SubscriptionPlan.Enterprise,
            cancellationToken);

        var secondaryTenant = await EnsureTenantAsync(
            dbContext,
            name: "Dev Wholesale",
            code: "dev-wholesale",
            plan: SubscriptionPlan.Growth,
            cancellationToken);

        var tertiaryTenant = await EnsureTenantAsync(
            dbContext,
            name: "Dev Starter",
            code: "dev-starter",
            plan: SubscriptionPlan.Starter,
            cancellationToken);

        var ensuredUsers = new List<bool>
        {
            await UpsertTestUserAsync(
                dbContext,
                passwordHasher,
                userName: "platform.admin",
                email: "platform.admin@erp.local",
                password: "Test123!",
                role: AppRoles.Admin,
                tenantId: null,
                cancellationToken),
            await UpsertTestUserAsync(
                dbContext,
                passwordHasher,
                userName: "test.admin",
                email: "test.admin@erp.local",
                password: "Test123!",
                role: AppRoles.Tier3,
                tenantId: primaryTenant.Id,
                cancellationToken),
            await UpsertTestUserAsync(
                dbContext,
                passwordHasher,
                userName: "test.employee",
                email: "test.employee@erp.local",
                password: "Test123!",
                role: AppRoles.Tier3,
                tenantId: primaryTenant.Id,
                cancellationToken),
            await UpsertTestUserAsync(
                dbContext,
                passwordHasher,
                userName: "test.admin.2",
                email: "test.admin.2@erp.local",
                password: "Test123!",
                role: AppRoles.Tier2,
                tenantId: secondaryTenant.Id,
                cancellationToken),
            await UpsertTestUserAsync(
                dbContext,
                passwordHasher,
                userName: "test.employee.2",
                email: "test.employee.2@erp.local",
                password: "Test123!",
                role: AppRoles.Tier2,
                tenantId: secondaryTenant.Id,
                cancellationToken),
            await UpsertTestUserAsync(
                dbContext,
                passwordHasher,
                userName: "test.employee.3",
                email: "test.employee.3@erp.local",
                password: "Test123!",
                role: AppRoles.Tier1,
                tenantId: tertiaryTenant.Id,
                cancellationToken)
        };

        if (ensuredUsers.Any(x => x))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Development users ensured for tenants: dev-retail, dev-wholesale, dev-starter and platform.admin.");
        }
    }

    private static async Task<TenantAccount> EnsureTenantAsync(
        ErpDbContext dbContext,
        string name,
        string code,
        SubscriptionPlan plan,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.TenantAccounts.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (tenant is null)
        {
            tenant = new TenantAccount
            {
                Name = name,
                Code = code,
                Plan = plan,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionStartAtUtc = DateTime.UtcNow,
                SubscriptionEndAtUtc = DateTime.UtcNow.AddMonths(1),
                MaxUsers = SubscriptionPlanCatalog.GetMaxUsers(plan)
            };

            dbContext.TenantAccounts.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return tenant;
    }

    private static async Task EnsureDefaultPlanSettingsAsync(ErpDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingPlans = await dbContext.SubscriptionPlanSettings
            .AsNoTracking()
            .Select(x => x.Plan)
            .ToListAsync(cancellationToken);

        var missing = Enum.GetValues<SubscriptionPlan>()
            .Where(plan => !existingPlans.Contains(plan))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        foreach (var plan in missing)
        {
            dbContext.SubscriptionPlanSettings.Add(new SubscriptionPlanSetting
            {
                Plan = plan,
                DisplayName = SubscriptionPlanCatalog.GetDisplayName(plan),
                MonthlyPrice = SubscriptionPlanCatalog.GetDefaultMonthlyPrice(plan),
                MaxUsers = SubscriptionPlanCatalog.GetMaxUsers(plan),
                FeaturesCsv = string.Join(',', SubscriptionPlanCatalog.GetFeatures(plan)),
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultLandingContentAsync(ErpDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.LandingPageContents.AnyAsync(cancellationToken))
        {
            return;
        }

        var defaults = new List<LandingPageContent>
        {
            new()
            {
                Key = "hero-title",
                Title = "Hero Title",
                Content = "Perakende satisinizi tek panelden yonetin.",
                SortOrder = 1,
                IsPublished = true
            },
            new()
            {
                Key = "hero-subtitle",
                Title = "Hero Subtitle",
                Content = "Stok, cari, pos ve raporlar tek bir ERP platformunda.",
                SortOrder = 2,
                IsPublished = true
            },
            new()
            {
                Key = "pricing-note",
                Title = "Pricing Note",
                Content = "Abonelik paketleri aylik olarak yenilenir.",
                SortOrder = 3,
                IsPublished = true
            }
        };

        dbContext.LandingPageContents.AddRange(defaults);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> UpsertTestUserAsync(
        ErpDbContext dbContext,
        IPasswordHasher passwordHasher,
        string userName,
        string email,
        string password,
        string role,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(
            x => x.UserName == userName,
            cancellationToken);

        if (existingUser is null)
        {
            dbContext.Users.Add(new AppUser
            {
                TenantAccountId = tenantId,
                UserName = userName,
                Email = email,
                PasswordHash = passwordHasher.Hash(password),
                Role = role
            });

            return true;
        }

        var hasChanges = false;

        if (!string.Equals(existingUser.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            existingUser.Email = email;
            hasChanges = true;
        }

        if (!string.Equals(existingUser.Role, role, StringComparison.OrdinalIgnoreCase))
        {
            existingUser.Role = role;
            hasChanges = true;
        }

        if (existingUser.TenantAccountId != tenantId)
        {
            existingUser.TenantAccountId = tenantId;
            hasChanges = true;
        }

        if (!passwordHasher.Verify(password, existingUser.PasswordHash))
        {
            existingUser.PasswordHash = passwordHasher.Hash(password);
            hasChanges = true;
        }

        if (hasChanges)
        {
            existingUser.UpdatedAtUtc = DateTime.UtcNow;
        }

        return hasChanges;
    }
}
