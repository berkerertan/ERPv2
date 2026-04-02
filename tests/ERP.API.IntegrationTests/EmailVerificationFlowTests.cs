using ERP.Domain.Constants;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class EmailVerificationFlowTests
{
    [Fact]
    public async Task ResendVerificationEmail_Should_Apply_Cooldown_Limit()
    {
        await using var factory = new ErpApiWebApplicationFactory(
            enforceAuthorization: true,
            additionalSettings: new Dictionary<string, string?>
            {
                ["EmailVerification:ResendCooldownSeconds"] = "3600",
                ["EmailVerification:DailyResendLimit"] = "5",
                ["EmailVerification:TokenTtlHours"] = "24"
            });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"cooldown-{Guid.NewGuid():N}@example.com";

        var registerResponse = await RegisterSaasAsync(client, email);
        registerResponse.EnsureSuccessStatusCode();

        var resendResponse = await client.PostAsJsonAsync("/api/auth/resend-verification-email", new { email });
        resendResponse.EnsureSuccessStatusCode();

        using var payload = JsonDocument.Parse(await resendResponse.Content.ReadAsStringAsync());
        Assert.False(payload.RootElement.GetProperty("isSent").GetBoolean());
        Assert.True(payload.RootElement.GetProperty("isRateLimited").GetBoolean());
        Assert.True(payload.RootElement.GetProperty("retryAfterSeconds").GetInt32() > 0);
    }

    [Fact]
    public async Task ConfirmEmail_WhenExpired_AndResendRequested_Should_Trigger_Resend()
    {
        await using var factory = new ErpApiWebApplicationFactory(
            enforceAuthorization: true,
            additionalSettings: new Dictionary<string, string?>
            {
                ["EmailVerification:ResendCooldownSeconds"] = "0",
                ["EmailVerification:DailyResendLimit"] = "5"
            });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"expired-{Guid.NewGuid():N}@example.com";

        var registerResponse = await RegisterSaasAsync(client, email);
        registerResponse.EnsureSuccessStatusCode();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
            var user = await dbContext.Users.FirstAsync(x => x.Email.ToLower() == email.ToLower());
            user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var confirmResponse = await client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            email,
            token = "expired-token",
            resendOnExpired = true
        });
        confirmResponse.EnsureSuccessStatusCode();

        using var payload = JsonDocument.Parse(await confirmResponse.Content.ReadAsStringAsync());
        Assert.False(payload.RootElement.GetProperty("isVerified").GetBoolean());
        Assert.True(payload.RootElement.GetProperty("isExpired").GetBoolean());
        Assert.True(payload.RootElement.GetProperty("resendTriggered").GetBoolean());
        Assert.True(payload.RootElement.GetProperty("resendSent").GetBoolean());
    }

    [Fact]
    public async Task VerificationDispatch_Should_Be_Auditable_In_Admin_Email_Logs()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"audit-{Guid.NewGuid():N}@example.com";

        var registerResponse = await RegisterSaasAsync(client, email);
        registerResponse.EnsureSuccessStatusCode();

        var adminToken = await LoginAsync(client, "platform.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var logsResponse = await client.GetAsync("/api/platform-admin/email/logs?q=account-email-verification&pageSize=200");
        logsResponse.EnsureSuccessStatusCode();

        using var payload = JsonDocument.Parse(await logsResponse.Content.ReadAsStringAsync());
        var match = payload.RootElement.EnumerateArray().Any(x =>
            string.Equals(x.GetProperty("templateKey").GetString(), EmailTemplateKeys.AccountVerification, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.GetProperty("recipientEmail").GetString(), email.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));

        Assert.True(match, "Expected verification email dispatch log was not found in admin email logs.");
    }

    [Fact]
    public async Task ExistingToken_Should_Be_Rejected_When_Email_Becomes_Unverified()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"guard-{Guid.NewGuid():N}@example.com";
        var userName = $"guard_{Guid.NewGuid():N}".Substring(0, 24);

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register-saas", new
        {
            userName,
            email,
            password = "Test123!",
            companyName = $"Guard Company {Guid.NewGuid():N}".Substring(0, 24),
            plan = 0
        });
        registerResponse.EnsureSuccessStatusCode();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
            var user = await dbContext.Users.FirstAsync(x => x.Email.ToLower() == email.ToLower());
            user.IsEmailConfirmed = true;
            user.EmailConfirmedAtUtc = DateTime.UtcNow;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var accessToken = await LoginAsync(client, userName, "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
            var user = await dbContext.Users.FirstAsync(x => x.Email.ToLower() == email.ToLower());
            user.IsEmailConfirmed = false;
            user.EmailConfirmedAtUtc = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Forbidden, meResponse.StatusCode);
    }

    private static Task<HttpResponseMessage> RegisterSaasAsync(HttpClient client, string email)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var userName = $"user_{suffix}".Substring(0, 24);
        var companyName = $"Company {suffix}".Substring(0, 24);

        return client.PostAsJsonAsync("/api/auth/register-saas", new
        {
            userName,
            email,
            password = "Test123!",
            companyName,
            plan = 0
        });
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }
}
