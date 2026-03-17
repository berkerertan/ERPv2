using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class MyActivityLogsTests
{
    [Fact]
    public async Task My_Activity_Logs_Should_Require_Authentication()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/activity-logs/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task My_Activity_Logs_Should_Return_Only_Current_User_Requests()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var demoClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var tier2Client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var demoToken = await LoginAsync(demoClient, "demo", "Test123!");
        var tier2Token = await LoginAsync(tier2Client, "demo.tier2", "Test123!");

        demoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", demoToken);
        tier2Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tier2Token);

        var demoUserId = await GetCurrentUserIdAsync(demoClient);
        var tier2UserId = await GetCurrentUserIdAsync(tier2Client);

        var demoUniqueResponse = await demoClient.GetAsync("/api/companies");
        demoUniqueResponse.EnsureSuccessStatusCode();

        var tier2UniqueResponse = await tier2Client.GetAsync("/api/warehouses");
        tier2UniqueResponse.EnsureSuccessStatusCode();

        var demoLogsResponse = await demoClient.GetAsync("/api/activity-logs/me?page=1&pageSize=300");
        demoLogsResponse.EnsureSuccessStatusCode();
        var demoUserIds = await DeserializeUserIdsAsync(demoLogsResponse);

        var tier2LogsResponse = await tier2Client.GetAsync("/api/activity-logs/me?page=1&pageSize=300");
        tier2LogsResponse.EnsureSuccessStatusCode();
        var tier2UserIds = await DeserializeUserIdsAsync(tier2LogsResponse);

        Assert.NotEmpty(demoUserIds);
        Assert.NotEmpty(tier2UserIds);

        Assert.All(demoUserIds, x => Assert.Equal(demoUserId, x));
        Assert.All(tier2UserIds, x => Assert.Equal(tier2UserId, x));
        Assert.DoesNotContain(demoUserIds, x => x == tier2UserId);
        Assert.DoesNotContain(tier2UserIds, x => x == demoUserId);
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("userId").GetGuid();
    }

    private static async Task<List<Guid>> DeserializeUserIdsAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        return document.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("userId").GetGuid())
            .ToList();
    }
}
