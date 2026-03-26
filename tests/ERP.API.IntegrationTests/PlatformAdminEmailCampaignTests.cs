using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class PlatformAdminEmailCampaignTests
{
    [Fact]
    public async Task Platform_Admin_Should_Create_Queue_And_List_Email_Campaign()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "platform.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenantId = await GetAnyTenantIdAsync(client);

        var previewResponse = await client.PostAsJsonAsync("/api/platform-admin/email/campaigns/preview", new
        {
            templateKey = "welcome",
            tenantIds = new[] { tenantId },
            sendToAllActiveTenants = false,
            sendToAllTenantUsers = false
        });
        previewResponse.EnsureSuccessStatusCode();

        var createResponse = await client.PostAsJsonAsync("/api/platform-admin/email/campaigns", new
        {
            name = "Test Campaign",
            description = "Integration test campaign",
            templateKey = "welcome",
            tenantIds = new[] { tenantId },
            sendToAllActiveTenants = false,
            sendToAllTenantUsers = false,
            subjectOverride = "Hos geldiniz {{TenantName}}",
            bodyOverride = "<p>Merhaba {{TenantName}}</p>"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var campaignId = createDoc.RootElement.GetProperty("id").GetGuid();

        var queueResponse = await client.PostAsync($"/api/platform-admin/email/campaigns/{campaignId}/queue", null);
        queueResponse.EnsureSuccessStatusCode();

        using var queueDoc = JsonDocument.Parse(await queueResponse.Content.ReadAsStringAsync());
        Assert.True(queueDoc.RootElement.GetProperty("totalRecipients").GetInt32() >= 1);

        var detailResponse = await client.GetAsync($"/api/platform-admin/email/campaigns/{campaignId}");
        detailResponse.EnsureSuccessStatusCode();

        var recipientsResponse = await client.GetAsync($"/api/platform-admin/email/campaigns/{campaignId}/recipients?page=1&pageSize=20");
        recipientsResponse.EnsureSuccessStatusCode();

        using var recipientsDoc = JsonDocument.Parse(await recipientsResponse.Content.ReadAsStringAsync());
        Assert.True(recipientsDoc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Platform_Admin_Should_Cancel_Scheduled_Email_Campaign()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "platform.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenantId = await GetAnyTenantIdAsync(client);
        var scheduleAtUtc = DateTime.UtcNow.AddDays(1);

        var createResponse = await client.PostAsJsonAsync("/api/platform-admin/email/campaigns", new
        {
            name = "Scheduled Campaign",
            templateKey = "reminder",
            tenantIds = new[] { tenantId },
            sendToAllActiveTenants = false,
            sendToAllTenantUsers = false,
            scheduledAtUtc = scheduleAtUtc
        });
        createResponse.EnsureSuccessStatusCode();

        using var createDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var campaignId = createDoc.RootElement.GetProperty("id").GetGuid();

        var queueResponse = await client.PostAsync($"/api/platform-admin/email/campaigns/{campaignId}/queue", null);
        queueResponse.EnsureSuccessStatusCode();

        var cancelResponse = await client.PostAsync($"/api/platform-admin/email/campaigns/{campaignId}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var detailResponse = await client.GetAsync($"/api/platform-admin/email/campaigns/{campaignId}");
        detailResponse.EnsureSuccessStatusCode();

        using var detailDoc = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var status = detailDoc.RootElement.GetProperty("status").GetInt32();
        Assert.Equal((int)PlatformEmailCampaignStatus.Cancelled, status);
    }

    [Fact]
    public async Task Tenant_User_Should_Not_Access_Email_Campaign_Endpoints()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/platform-admin/email/campaigns");
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
            throw new InvalidOperationException("No tenant found for email campaign test.");
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
