using ERP.Application.Abstractions.Security;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
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

        var adminEnsured = await UpsertTestUserAsync(
            dbContext,
            passwordHasher,
            userName: "test.admin",
            email: "test.admin@erp.local",
            password: "Test123!",
            role: AppRoles.Admin,
            cancellationToken);

        var employeeEnsured = await UpsertTestUserAsync(
            dbContext,
            passwordHasher,
            userName: "test.employee",
            email: "test.employee@erp.local",
            password: "Test123!",
            role: AppRoles.Employee,
            cancellationToken);

        if (adminEnsured || employeeEnsured)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Development test users ensured: test.admin (Admin), test.employee (Employee)");
        }
    }

    private static async Task<bool> UpsertTestUserAsync(
        ErpDbContext dbContext,
        IPasswordHasher passwordHasher,
        string userName,
        string email,
        string password,
        string role,
        CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(
            x => x.UserName == userName,
            cancellationToken);

        if (existingUser is null)
        {
            dbContext.Users.Add(new AppUser
            {
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

