using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class ProductBarcodeScanTests
{
    [Fact]
    public async Task Scan_Barcode_Should_Return_Found_Product_When_Exists()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var barcode = "8690001234567";
        var productId = await CreateProductAsync(client, $"SCAN-{Guid.NewGuid():N}".Substring(0, 18), "Scan Product", barcode);

        var response = await client.GetAsync($"/api/products/scan?barcode={barcode}");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.GetProperty("found").GetBoolean());
        Assert.Equal(barcode, root.GetProperty("barcode").GetString());
        Assert.Equal(productId, root.GetProperty("product").GetProperty("id").GetGuid());
        Assert.Equal("Scan Product", root.GetProperty("product").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Scan_Barcode_Should_Return_Draft_When_Not_Exists()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var barcode = "8900000000001";
        var response = await client.GetAsync($"/api/products/scan?barcode={barcode}");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.False(root.GetProperty("found").GetBoolean());
        Assert.Equal(barcode, root.GetProperty("barcode").GetString());
        Assert.True(root.TryGetProperty("draft", out var draft));
        Assert.Equal(barcode, draft.GetProperty("barcodeEan13").GetString());
        Assert.Equal("EA", draft.GetProperty("unit").GetString());
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string code, string name, string barcodeEan13)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            code,
            name,
            unit = "EA",
            category = "Scan",
            barcodeEan13,
            defaultSalePrice = 10m,
            criticalStockLevel = 1m
        });
        response.EnsureSuccessStatusCode();
        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> DeserializeGuidAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<Guid>(payload);
        return value == Guid.Empty ? throw new InvalidOperationException("Expected a valid Guid response.") : value;
    }
}
