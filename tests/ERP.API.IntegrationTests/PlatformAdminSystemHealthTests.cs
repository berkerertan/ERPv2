using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class PlatformAdminSystemHealthTests
{
    [Fact]
    public async Task Platform_Admin_Should_Get_System_Health_Overview_And_Timeline()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "platform.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var overviewResponse = await client.GetAsync("/api/platform-admin/system-health/overview");
        overviewResponse.EnsureSuccessStatusCode();

        using var overviewDoc = JsonDocument.Parse(await overviewResponse.Content.ReadAsStringAsync());
        Assert.True(overviewDoc.RootElement.TryGetProperty("status", out _));
        Assert.True(overviewDoc.RootElement.TryGetProperty("uptimeSeconds", out _));
        Assert.True(overviewDoc.RootElement.TryGetProperty("databaseReachable", out _));

        var timelineResponse = await client.GetAsync("/api/platform-admin/system-health/timeline?minutes=60&bucketMinutes=5");
        timelineResponse.EnsureSuccessStatusCode();

        using var timelineDoc = JsonDocument.Parse(await timelineResponse.Content.ReadAsStringAsync());
        Assert.True(timelineDoc.RootElement.TryGetProperty("rangeMinutes", out _));
        Assert.True(timelineDoc.RootElement.TryGetProperty("points", out var points));
        Assert.True(points.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Tenant_User_Should_Not_Get_System_Health_Endpoints()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/platform-admin/system-health/overview");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
