using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class ProfileSettingsTests
{
    [Fact]
    public async Task Update_Me_Should_Persist_UserName_And_Email()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUserName = $"demo_{Guid.NewGuid():N}".Substring(0, 20);
        var newEmail = $"{newUserName}@example.com";

        var updateResponse = await client.PutAsJsonAsync("/api/auth/me", new
        {
            userName = newUserName,
            email = newEmail
        });
        updateResponse.EnsureSuccessStatusCode();

        using var updatedDoc = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal(newUserName, updatedDoc.RootElement.GetProperty("userName").GetString());
        Assert.Equal(newEmail, updatedDoc.RootElement.GetProperty("email").GetString());

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();

        using var meDoc = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        Assert.Equal(newUserName, meDoc.RootElement.GetProperty("userName").GetString());
        Assert.Equal(newEmail, meDoc.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Change_Me_Password_Should_Reject_Old_And_Accept_New_Credentials()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newPassword = "Test123!_new";
        var changePasswordResponse = await client.PutAsJsonAsync("/api/auth/me/password", new
        {
            currentPassword = "Test123!",
            newPassword = newPassword
        });

        Assert.Equal(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;

        var oldLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "demo",
            password = "Test123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        var newLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "demo",
            password = newPassword
        });
        Assert.True(
            newLoginResponse.IsSuccessStatusCode,
            $"Expected successful login with new password but got {(int)newLoginResponse.StatusCode}: {await newLoginResponse.Content.ReadAsStringAsync()}");
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
