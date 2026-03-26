using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class PlatformAdminEmailOperationsTests
{
    [Fact]
    public async Task Platform_Admin_Should_Manage_Email_Templates()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "platform.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var listResponse = await client.GetAsync("/api/platform-admin/email/templates");
        listResponse.EnsureSuccessStatusCode();

        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var hasWelcomeTemplate = listDoc.RootElement
            .EnumerateArray()
            .Any(x => string.Equals(x.GetProperty("key").GetString(), "welcome", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasWelcomeTemplate);

        var updateResponse = await client.PutAsJsonAsync("/api/platform-admin/email/templates/welcome", new
        {
            name = "Welcome Mail",
            subjectTemplate = "Hos geldiniz {{TenantName}}",
            bodyTemplate = "<p>Merhaba {{TenantName}}</p><p>Kod: {{TenantCode}}</p>",
            description = "Welcome flow",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var detailResponse = await client.GetAsync("/api/platform-admin/email/templates/welcome");
        detailResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Platform_Admin_Should_Send_Tenant_Email_And_Write_Log()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "platform.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenantId = await GetAnyTenantIdAsync(client);

        var sendResponse = await client.PostAsJsonAsync("/api/platform-admin/email/send", new
        {
            templateKey = "welcome",
            tenantIds = new[] { tenantId },
            sendToAllActiveTenants = false,
            sendToAllTenantUsers = false
        });
        sendResponse.EnsureSuccessStatusCode();

        using var sendDoc = JsonDocument.Parse(await sendResponse.Content.ReadAsStringAsync());
        Assert.True(sendDoc.RootElement.TryGetProperty("processedTenantCount", out var processed));
        Assert.True(processed.GetInt32() >= 1);

        var logsResponse = await client.GetAsync("/api/platform-admin/email/logs?page=1&pageSize=20");
        logsResponse.EnsureSuccessStatusCode();

        using var logsDoc = JsonDocument.Parse(await logsResponse.Content.ReadAsStringAsync());
        var hasWelcomeLog = logsDoc.RootElement
            .EnumerateArray()
            .Any(x => string.Equals(x.GetProperty("templateKey").GetString(), "welcome", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasWelcomeLog);
    }

    [Fact]
    public async Task Tenant_User_Should_Not_Access_Platform_Admin_Email_Endpoints()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/platform-admin/email/templates");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<Guid> GetAnyTenantIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/platform-admin/subscribers?page=1&pageSize=5");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = doc.RootElement.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object || !first.TryGetProperty("tenantId", out var idProperty))
        {
            throw new InvalidOperationException("No tenant found for admin email test.");
        }

        return idProperty.GetGuid();
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }
}
