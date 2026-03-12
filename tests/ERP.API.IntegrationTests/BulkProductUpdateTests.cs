using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class BulkProductUpdateTests
{
    [Fact]
    public async Task Bulk_Price_And_Stock_Update_Should_Work_For_Current_Tenant()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "test.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var companyId = await CreateCompanyAsync(client);
        var branchId = await CreateBranchAsync(client, companyId);
        var warehouseId = await CreateWarehouseAsync(client, branchId);

        var firstProductId = await CreateProductAsync(client, $"BULK-P-{Guid.NewGuid():N}".Substring(0, 18), "Bulk Product 1");
        var secondProductId = await CreateProductAsync(client, $"BULK-P-{Guid.NewGuid():N}".Substring(0, 18), "Bulk Product 2");

        var bulkPriceResponse = await client.PostAsJsonAsync("/api/products/bulk-price-update", new
        {
            items = new[]
            {
                new { productId = firstProductId, defaultSalePrice = 99.50m },
                new { productId = secondProductId, defaultSalePrice = 149.75m }
            }
        });
        bulkPriceResponse.EnsureSuccessStatusCode();

        using (var priceDoc = JsonDocument.Parse(await bulkPriceResponse.Content.ReadAsStringAsync()))
        {
            var root = priceDoc.RootElement;
            Assert.Equal(2, root.GetProperty("requested").GetInt32());
            Assert.Equal(2, root.GetProperty("updated").GetInt32());
            Assert.Equal(0, root.GetProperty("notFound").GetInt32());
        }

        var productAfterPriceUpdate = await client.GetAsync($"/api/products/{firstProductId}");
        productAfterPriceUpdate.EnsureSuccessStatusCode();
        using (var productDoc = JsonDocument.Parse(await productAfterPriceUpdate.Content.ReadAsStringAsync()))
        {
            var price = productDoc.RootElement.GetProperty("defaultSalePrice").GetDecimal();
            Assert.Equal(99.50m, price);
        }

        var bulkStockResponse = await client.PostAsJsonAsync("/api/products/bulk-stock-update", new
        {
            warehouseId,
            referenceNo = $"BULK-STOCK-{DateTime.UtcNow:yyyyMMddHHmmss}",
            items = new[]
            {
                new { productId = firstProductId, quantityDelta = 10m, unitPrice = 50m },
                new { productId = secondProductId, quantityDelta = 6m, unitPrice = 70m }
            }
        });
        bulkStockResponse.EnsureSuccessStatusCode();

        using (var stockDoc = JsonDocument.Parse(await bulkStockResponse.Content.ReadAsStringAsync()))
        {
            var root = stockDoc.RootElement;
            Assert.Equal(2, root.GetProperty("requested").GetInt32());
            Assert.Equal(2, root.GetProperty("movementsCreated").GetInt32());
            Assert.Equal(0, root.GetProperty("notFound").GetInt32());
            Assert.Equal(0, root.GetProperty("skippedZeroQuantity").GetInt32());
        }

        var balancesResponse = await client.GetAsync("/api/stock-movements/balances");
        balancesResponse.EnsureSuccessStatusCode();
        using var balancesDoc = JsonDocument.Parse(await balancesResponse.Content.ReadAsStringAsync());
        var hasFirstProductBalance = balancesDoc.RootElement.EnumerateArray()
            .Any(x =>
                x.GetProperty("productId").GetGuid() == firstProductId &&
                x.GetProperty("balance").GetDecimal() >= 10m);

        Assert.True(hasFirstProductBalance);
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }

    private static async Task<Guid> CreateCompanyAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/companies", new
        {
            code = $"CMP{DateTime.UtcNow:HHmmss}",
            name = "Bulk Company",
            taxNumber = $"T{DateTime.UtcNow:mmssfff}"
        });
        response.EnsureSuccessStatusCode();
        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateBranchAsync(HttpClient client, Guid companyId)
    {
        var response = await client.PostAsJsonAsync("/api/branches", new
        {
            companyId,
            code = $"BR{DateTime.UtcNow:HHmmss}",
            name = "Bulk Branch"
        });
        response.EnsureSuccessStatusCode();
        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateWarehouseAsync(HttpClient client, Guid branchId)
    {
        var response = await client.PostAsJsonAsync("/api/warehouses", new
        {
            branchId,
            code = $"WH{DateTime.UtcNow:HHmmss}",
            name = "Bulk Warehouse"
        });
        response.EnsureSuccessStatusCode();
        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            code,
            name,
            unit = "EA",
            category = "Bulk",
            barcodeEan13 = (string?)null,
            qrCode = (string?)null,
            defaultSalePrice = 12.5m,
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
