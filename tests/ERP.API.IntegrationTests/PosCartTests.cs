using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class PosCartTests
{
    [Fact]
    public async Task Save_List_ByToken_And_Delete_Should_Work()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var publicClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var companyId = await CreateCompanyAsync(client);
        var branchId = await CreateBranchAsync(client, companyId);
        var warehouseId = await CreateWarehouseAsync(client, branchId);

        var saveResponse = await client.PostAsJsonAsync("/api/PosCart/Save", new
        {
            label = "Test Sepet 1",
            paymentMethod = "cash",
            warehouseId,
            items = new[]
            {
                new
                {
                    productId = (Guid?)null,
                    name = "Demo Product",
                    barcode = "123456",
                    quantity = 2m,
                    unitPrice = 55m,
                    total = 110m
                }
            }
        });
        saveResponse.EnsureSuccessStatusCode();

        using var saveDoc = JsonDocument.Parse(await saveResponse.Content.ReadAsStringAsync());
        var savedCartId = saveDoc.RootElement.GetProperty("id").GetGuid();
        var shareToken = saveDoc.RootElement.GetProperty("shareToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(shareToken));

        var listResponse = await client.GetAsync("/api/PosCart/List");
        listResponse.EnsureSuccessStatusCode();

        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var listedCart = listDoc.RootElement.EnumerateArray().FirstOrDefault(x => x.GetProperty("id").GetGuid() == savedCartId);
        Assert.Equal(savedCartId, listedCart.GetProperty("id").GetGuid());
        Assert.Equal(1, listedCart.GetProperty("itemCount").GetInt32());
        Assert.Equal(110m, listedCart.GetProperty("grandTotal").GetDecimal());

        var byTokenResponse = await publicClient.GetAsync($"/api/PosCart/ByToken/{shareToken}");
        byTokenResponse.EnsureSuccessStatusCode();

        using var byTokenDoc = JsonDocument.Parse(await byTokenResponse.Content.ReadAsStringAsync());
        Assert.Equal(savedCartId, byTokenDoc.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Test Sepet 1", byTokenDoc.RootElement.GetProperty("label").GetString());

        var deleteResponse = await client.DeleteAsync($"/api/PosCart/Delete/{savedCartId}");
        deleteResponse.EnsureSuccessStatusCode();

        var deletedByTokenResponse = await publicClient.GetAsync($"/api/PosCart/ByToken/{shareToken}");
        Assert.Equal(HttpStatusCode.NotFound, deletedByTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Save_Should_Keep_Only_Latest_Five_Carts()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var publicClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var companyId = await CreateCompanyAsync(client);
        var branchId = await CreateBranchAsync(client, companyId);
        var warehouseId = await CreateWarehouseAsync(client, branchId);

        Guid firstCartId = Guid.Empty;
        string firstShareToken = string.Empty;
        var savedIds = new List<Guid>(6);

        for (var i = 1; i <= 6; i++)
        {
            var saveResponse = await client.PostAsJsonAsync("/api/PosCart/Save", new
            {
                label = $"Cart {i}",
                paymentMethod = "cash",
                warehouseId,
                items = new[]
                {
                    new
                    {
                        productId = (Guid?)null,
                        name = $"Product {i}",
                        barcode = $"BRC{i:000}",
                        quantity = 1m,
                        unitPrice = 10m + i,
                        total = 10m + i
                    }
                }
            });
            saveResponse.EnsureSuccessStatusCode();

            using var saveDoc = JsonDocument.Parse(await saveResponse.Content.ReadAsStringAsync());
            var cartId = saveDoc.RootElement.GetProperty("id").GetGuid();
            var shareToken = saveDoc.RootElement.GetProperty("shareToken").GetString() ?? string.Empty;
            savedIds.Add(cartId);

            if (i == 1)
            {
                firstCartId = cartId;
                firstShareToken = shareToken;
            }
        }

        var listResponse = await client.GetAsync("/api/PosCart/List");
        listResponse.EnsureSuccessStatusCode();

        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var idsFromList = listDoc.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("id").GetGuid())
            .ToList();

        Assert.Equal(5, idsFromList.Count);
        Assert.DoesNotContain(firstCartId, idsFromList);
        Assert.Contains(savedIds[^1], idsFromList);

        var deletedByTokenResponse = await publicClient.GetAsync($"/api/PosCart/ByToken/{firstShareToken}");
        Assert.Equal(HttpStatusCode.NotFound, deletedByTokenResponse.StatusCode);
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
            code = $"PC{DateTime.UtcNow:HHmmss}",
            name = "Pos Cart Company",
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
            code = $"PB{DateTime.UtcNow:HHmmss}",
            name = "Pos Cart Branch"
        });
        response.EnsureSuccessStatusCode();
        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateWarehouseAsync(HttpClient client, Guid branchId)
    {
        var response = await client.PostAsJsonAsync("/api/warehouses", new
        {
            branchId,
            code = $"PW{DateTime.UtcNow:HHmmss}",
            name = "Pos Cart Warehouse"
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
