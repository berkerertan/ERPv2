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

        var credentials = await RegisterAndLoginAsync(client, "profile");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        var newUserName = $"profile_{Guid.NewGuid():N}".Substring(0, 20);
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

        var credentials = await RegisterAndLoginAsync(client, "password");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        var newPassword = "Test123!_new";
        var changePasswordResponse = await client.PutAsJsonAsync("/api/auth/me/password", new
        {
            currentPassword = credentials.Password,
            newPassword = newPassword
        });

        Assert.Equal(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;

        var oldLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = credentials.UserName,
            password = credentials.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        var newLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = credentials.UserName,
            password = newPassword
        });
        Assert.True(
            newLoginResponse.IsSuccessStatusCode,
            $"Expected successful login with new password but got {(int)newLoginResponse.StatusCode}: {await newLoginResponse.Content.ReadAsStringAsync()}");
    }

    private static async Task<TestUserCredentials> RegisterAndLoginAsync(HttpClient client, string prefix)
    {
        var userName = $"{prefix}_{Guid.NewGuid():N}".Substring(0, 20);
        var email = $"{userName}@example.com";
        const string password = "Test123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userName,
            email,
            password,
            role = "1.Kademe"
        });
        registerResponse.EnsureSuccessStatusCode();

        var token = await LoginAsync(client, userName, password);
        return new TestUserCredentials(userName, password, token);
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }

    private sealed record TestUserCredentials(string UserName, string Password, string AccessToken);
}
