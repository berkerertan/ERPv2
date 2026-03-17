using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class AuthorizationAndTenantIsolationTests
{
    [Fact]
    public async Task Protected_Endpoint_Should_Return_Unauthorized_When_Auth_Is_Enforced()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Work_Without_Token_When_Auth_Is_Disabled()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_User_Should_Not_Access_Platform_Admin_Endpoints()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/platform-admin/subscribers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_User_Should_Not_Access_Platform_Admin_Endpoints_When_Auth_Is_Disabled()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/platform-admin/subscribers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Platform_Admin_Endpoint_Should_Return_Unauthorized_Without_Token_When_Auth_Is_Disabled()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/platform-admin/subscribers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_Isolation_Should_Prevent_Cross_Tenant_Product_Read()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var retailClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var wholesaleClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var retailToken = await LoginAsync(retailClient, "demo", "Test123!");
        var wholesaleToken = await LoginAsync(wholesaleClient, "demo.tier2", "Test123!");

        retailClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", retailToken);
        wholesaleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wholesaleToken);

        var createResponse = await retailClient.PostAsJsonAsync("/api/products", new
        {
            code = $"TENANT-P-{Guid.NewGuid():N}".Substring(0, 18),
            name = "Tenant Isolated Product",
            unit = "EA",
            category = "Test",
            barcodeEan13 = (string?)null,
            qrCode = (string?)null,
            defaultSalePrice = 18.75m,
            criticalStockLevel = 2m
        });

        createResponse.EnsureSuccessStatusCode();
        var createdProductId = await DeserializeGuidAsync(createResponse);

        var otherTenantRead = await wholesaleClient.GetAsync($"/api/products/{createdProductId}");
        Assert.Equal(HttpStatusCode.NotFound, otherTenantRead.StatusCode);

        wholesaleClient.DefaultRequestHeaders.Remove("X-Tenant-Code");
        wholesaleClient.DefaultRequestHeaders.Add("X-Tenant-Code", "demo-tier3");

        var bypassAttempt = await wholesaleClient.GetAsync($"/api/products/{createdProductId}");
        Assert.Equal(HttpStatusCode.NotFound, bypassAttempt.StatusCode);
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
