using ClosedXML.Excel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class CariDebtItemsExportTests
{
    [Fact]
    public async Task Export_Debt_Items_As_Excel_Should_Return_Xlsx_File()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var buyerId = await GetAnyBuyerIdAsync(client);
        var response = await client.GetAsync($"/api/cari-accounts/{buyerId}/debt-items/export-excel");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        Assert.Equal("TransactionDate", sheet.Cell(1, 1).GetString());
        Assert.Equal("MaterialDescription", sheet.Cell(1, 2).GetString());
    }

    [Fact]
    public async Task Export_Debt_Items_As_Pdf_Should_Return_Pdf_File()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var buyerId = await GetAnyBuyerIdAsync(client);
        var response = await client.GetAsync($"/api/cari-accounts/{buyerId}/debt-items/export-pdf");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    private static async Task<Guid> GetAnyBuyerIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/cari-accounts/buyers?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = document.RootElement.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object || !first.TryGetProperty("id", out var idProperty))
        {
            throw new InvalidOperationException("No buyer account found for export test.");
        }

        return idProperty.GetGuid();
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
