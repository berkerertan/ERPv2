using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class AnnouncementsTests
{
    [Fact]
    public async Task Platform_Admin_Announcement_Should_Be_Visible_To_Tenant_Users()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var adminClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var tenantClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var adminToken = await LoginAsync(adminClient, "platform.admin", "Test123!");
        var tenantToken = await LoginAsync(tenantClient, "test.admin", "Test123!");

        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        tenantClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantToken);

        var title = $"Bakim Duyurusu {Guid.NewGuid():N}".Substring(0, 30);
        var createResponse = await adminClient.PostAsJsonAsync("/api/platform-admin/announcements", new
        {
            title,
            content = "Sistem bakimi gece 02:00-03:00 arasi planlanmistir.",
            isPublished = true,
            priority = 10,
            startsAtUtc = DateTime.UtcNow.AddMinutes(-1),
            endsAtUtc = (DateTime?)null
        });
        createResponse.EnsureSuccessStatusCode();
        var announcementId = await DeserializeGuidAsync(createResponse);

        var listResponse = await tenantClient.GetAsync("/api/announcements");
        listResponse.EnsureSuccessStatusCode();

        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var foundInList = listDoc.RootElement.EnumerateArray().Any(x => x.GetProperty("id").GetGuid() == announcementId);
        Assert.True(foundInList);

        var detailResponse = await tenantClient.GetAsync($"/api/announcements/{announcementId}");
        detailResponse.EnsureSuccessStatusCode();
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }

    private static async Task<Guid> DeserializeGuidAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<Guid>(payload);
        return value == Guid.Empty ? throw new InvalidOperationException("Expected a valid Guid response.") : value;
    }
}
