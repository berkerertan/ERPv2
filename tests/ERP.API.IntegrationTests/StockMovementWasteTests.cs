using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class StockMovementWasteTests
{
    [Fact]
    public async Task Waste_Scrap_Movement_Should_Require_Reason_And_Proof()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var companyId = await CreateCompanyAsync(client);
        var branchId = await CreateBranchAsync(client, companyId);
        var warehouseId = await CreateWarehouseAsync(client, branchId);
        var productId = await CreateProductAsync(client, $"WSR-{Guid.NewGuid():N}".Substring(0, 14), "Waste Required Fields Product");

        var badWasteResponse = await client.PostAsJsonAsync("/api/stock-movements", new
        {
            warehouseId,
            productId,
            type = 2, // Out
            reason = 8, // WasteScrap
            quantity = 1m,
            unitPrice = 10m,
            referenceNo = "TEST-WASTE-MISSING"
        });

        Assert.Equal(System.Net.HttpStatusCode.Conflict, badWasteResponse.StatusCode);
    }

    [Fact]
    public async Task Waste_Scrap_Movement_Should_Reduce_Balance_And_Be_Filterable_By_Reason()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var companyId = await CreateCompanyAsync(client);
        var branchId = await CreateBranchAsync(client, companyId);
        var warehouseId = await CreateWarehouseAsync(client, branchId);
        var productId = await CreateProductAsync(client, $"WS-{Guid.NewGuid():N}".Substring(0, 14), "Waste Test Product");

        var stockInResponse = await client.PostAsJsonAsync("/api/stock-movements", new
        {
            warehouseId,
            productId,
            type = 1, // In
            reason = 7, // InventoryAdjustment
            reasonNote = "Acil sayim duzeltmesi",
            quantity = 10m,
            unitPrice = 100m,
            referenceNo = "TEST-IN"
        });
        stockInResponse.EnsureSuccessStatusCode();

        var wasteResponse = await client.PostAsJsonAsync("/api/stock-movements", new
        {
            warehouseId,
            productId,
            type = 2, // Out
            reason = 8, // WasteScrap
            reasonNote = "Depoda kirik urun tespiti",
            proofImageUrl = "https://example.com/proof/waste-1.jpg",
            quantity = 3m,
            unitPrice = 100m,
            referenceNo = "TEST-WASTE"
        });
        wasteResponse.EnsureSuccessStatusCode();
        var wasteMovementId = await DeserializeGuidAsync(wasteResponse);

        var balancesResponse = await client.GetAsync("/api/stock-movements/balances");
        balancesResponse.EnsureSuccessStatusCode();
        using (var balancesDoc = JsonDocument.Parse(await balancesResponse.Content.ReadAsStringAsync()))
        {
            var matched = balancesDoc.RootElement.EnumerateArray()
                .Where(x => x.GetProperty("productId").GetGuid() == productId)
                .Sum(x => x.GetProperty("balance").GetDecimal());

            Assert.Equal(7m, matched);
        }

        var wasteListResponse = await client.GetAsync("/api/stock-movements?reason=8");
        wasteListResponse.EnsureSuccessStatusCode();
        using var wasteListDoc = JsonDocument.Parse(await wasteListResponse.Content.ReadAsStringAsync());

        var wasteMovement = wasteListDoc.RootElement.EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("id").GetGuid() == wasteMovementId);

        Assert.Equal(wasteMovementId, wasteMovement.GetProperty("id").GetGuid());
        Assert.Equal(8, wasteMovement.GetProperty("reason").GetInt32());
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
            code = $"WSC{DateTime.UtcNow:HHmmss}",
            name = "Waste Test Company",
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
            code = $"WSB{DateTime.UtcNow:HHmmss}",
            name = "Waste Test Branch"
        });
        response.EnsureSuccessStatusCode();
        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateWarehouseAsync(HttpClient client, Guid branchId)
    {
        var response = await client.PostAsJsonAsync("/api/warehouses", new
        {
            branchId,
            code = $"WSW{DateTime.UtcNow:HHmmss}",
            name = "Waste Test Warehouse"
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
            category = "Waste",
            barcodeEan13 = (string?)null,
            qrCode = (string?)null,
            defaultSalePrice = 120m,
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
