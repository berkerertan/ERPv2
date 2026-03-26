using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class AuthRefreshFlowTests
{
    [Fact]
    public async Task Login_Response_Should_Contain_Expiry_Fields_For_Frontend_Compatibility()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "platform.admin",
            password = "Test123!"
        });
        loginResponse.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("accessToken", out _));
        Assert.True(doc.RootElement.TryGetProperty("refreshToken", out _));
        Assert.True(doc.RootElement.TryGetProperty("accessTokenExpiresAtUtc", out _));
        Assert.True(doc.RootElement.TryGetProperty("expiresAtUtc", out _));
        Assert.True(doc.RootElement.TryGetProperty("refreshTokenExpiresAtUtc", out _));
    }

    [Fact]
    public async Task Refresh_Should_Rotate_RefreshToken_And_Old_RefreshToken_Should_Fail()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "platform.admin",
            password = "Test123!"
        });
        loginResponse.EnsureSuccessStatusCode();

        using var loginDoc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var oldRefreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()
            ?? throw new InvalidOperationException("Missing refresh token.");

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = oldRefreshToken
        });
        refreshResponse.EnsureSuccessStatusCode();

        using var refreshDoc = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        var newAccessToken = refreshDoc.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Missing access token.");
        var newRefreshToken = refreshDoc.RootElement.GetProperty("refreshToken").GetString()
            ?? throw new InvalidOperationException("Missing refresh token.");

        Assert.NotEqual(oldRefreshToken, newRefreshToken);

        var oldRefreshReuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = oldRefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshReuseResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
    }
}
