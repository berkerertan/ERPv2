using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class InvoiceDetailEndpointsTests
{
    [Fact]
    public async Task Invoice_Detail_Should_Return_Header_And_Items()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var productId = await CreateProductAsync(client, $"INV-P-{ts}", $"Invoice Product {ts}");
        var cariId = await CreateCariAsync(client, $"INV-C-{ts}", $"Invoice Cari {ts}");
        var invoiceId = await CreateInvoiceAsync(client, productId, cariId, $"INV-{ts}");

        var detailResponse = await client.GetAsync($"/api/invoices/{invoiceId}/detail");
        detailResponse.EnsureSuccessStatusCode();

        using var detailDoc = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var root = detailDoc.RootElement;

        Assert.Equal(invoiceId, root.GetProperty("invoice").GetProperty("id").GetGuid());
        Assert.True(root.GetProperty("items").GetArrayLength() >= 1);
        Assert.Equal("1234567890", root.GetProperty("invoice").GetProperty("taxNumber").GetString());
    }

    [Fact]
    public async Task Invoice_Preview_Html_Should_Return_Html_Document()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var productId = await CreateProductAsync(client, $"INV-PV-{ts}", $"Invoice Preview Product {ts}");
        var cariId = await CreateCariAsync(client, $"INV-CV-{ts}", $"Invoice Preview Cari {ts}");
        var invoiceId = await CreateInvoiceAsync(client, productId, cariId, $"INV-PV-{ts}");

        var previewResponse = await client.GetAsync($"/api/invoices/{invoiceId}/preview-html");
        previewResponse.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", previewResponse.Content.Headers.ContentType?.ToString());

        var html = await previewResponse.Content.ReadAsStringAsync();
        Assert.Contains("E-FATURA", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"INV-PV-{ts}", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            code,
            name,
            unit = "EA",
            category = "QA",
            barcodeEan13 = (string?)null,
            qrCode = (string?)null,
            defaultSalePrice = 10.5m,
            criticalStockLevel = 1m
        });
        response.EnsureSuccessStatusCode();

        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateCariAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/cari-accounts", new
        {
            code,
            name,
            type = 2,
            riskLimit = 1000m,
            maturityDays = 30,
            phone = "0555 123 45 67"
        });
        response.EnsureSuccessStatusCode();

        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateInvoiceAsync(HttpClient client, Guid productId, Guid cariId, string invoiceNumber)
    {
        var response = await client.PostAsJsonAsync("/api/invoices", new
        {
            invoiceNumber,
            invoiceType = 1,
            invoiceCategory = 1,
            cariAccountId = cariId,
            taxNumber = "1234567890",
            issueDate = DateTime.UtcNow,
            currency = "TRY",
            items = new[]
            {
                new
                {
                    productId,
                    quantity = 1m,
                    unitPrice = 10.5m,
                    taxRate = 20m,
                    discountRate = 0m
                }
            }
        });
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> DeserializeGuidAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<Guid>(payload);
        return value == Guid.Empty ? throw new InvalidOperationException("Expected a valid Guid response.") : value;
    }
}
