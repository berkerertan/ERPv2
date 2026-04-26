using ERP.Application.Abstractions.Security;
using ERP.Domain.Constants;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;

namespace ERP.API.Common;

public static class ProductionDemoAccountSeeder
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    public static async Task SeedAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        var enabled = app.Configuration.GetValue("DemoAccount:Enabled", true);
        if (!enabled)
        {
            return;
        }

        var password = app.Configuration["DemoAccount:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Test123!";
        }

        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<ErpDbContext>();
            var passwordHasher = services.GetRequiredService<IPasswordHasher>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ProductionDemoAccountSeeder");

            await DevelopmentDataSeeder.EnsureDefaultPlanSettingsAsync(dbContext, cancellationToken);

            var demoTier3Tenant = await DevelopmentDataSeeder.EnsureTenantAsync(
                dbContext,
                name: "Demo Enterprise",
                code: "demo-tier3",
                plan: SubscriptionPlan.Enterprise,
                cancellationToken);

            var demoTier2Tenant = await DevelopmentDataSeeder.EnsureTenantAsync(
                dbContext,
                name: "Demo Growth",
                code: "demo-tier2",
                plan: SubscriptionPlan.Growth,
                cancellationToken);

            var demoTier1Tenant = await DevelopmentDataSeeder.EnsureTenantAsync(
                dbContext,
                name: "Demo Starter",
                code: "demo-tier1",
                plan: SubscriptionPlan.Starter,
                cancellationToken);

            var usersChanged = new List<bool>
            {
                await DevelopmentDataSeeder.UpsertUserAsync(
                    dbContext,
                    passwordHasher,
                    userName: "demo",
                    email: "demo@stoknet.local",
                    password: password,
                    role: AppRoles.Tier3,
                    tenantId: demoTier3Tenant.Id,
                    cancellationToken),

                await DevelopmentDataSeeder.UpsertUserAsync(
                    dbContext,
                    passwordHasher,
                    userName: "demo.tier2",
                    email: "demo.tier2@stoknet.local",
                    password: password,
                    role: AppRoles.Tier2,
                    tenantId: demoTier2Tenant.Id,
                    cancellationToken),

                await DevelopmentDataSeeder.UpsertUserAsync(
                    dbContext,
                    passwordHasher,
                    userName: "demo.tier1",
                    email: "demo.tier1@stoknet.local",
                    password: password,
                    role: AppRoles.Tier1,
                    tenantId: demoTier1Tenant.Id,
                    cancellationToken)
            };

            if (usersChanged.Any(static changed => changed))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Production demo users ensured: demo, demo.tier2, demo.tier1.");
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }
}
