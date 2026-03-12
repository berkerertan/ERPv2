using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class BuyersBulkExcelImportTests
{
    [Fact]
    public async Task Buyers_Bulk_Excel_Import_Should_Create_Accounts_From_File_Names()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "test.admin", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var firstBuyerName = $"Ali Veli {suffix}";
        var secondBuyerName = $"Berker Ertan {suffix}";

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("false"), "ReplaceExisting");
        multipart.Add(new ByteArrayContent(BuildDebtExcelBytes()), "Files", $"{firstBuyerName}.xlsx");
        multipart.Add(new ByteArrayContent(BuildDebtExcelBytes()), "Files", $"{secondBuyerName}.xlsx");

        var response = await client.PostAsync("/api/cari-accounts/buyers/import-excel", multipart);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(2, root.GetProperty("totalFiles").GetInt32());
        Assert.Equal(2, root.GetProperty("processedFiles").GetInt32());
        Assert.True(root.GetProperty("createdCariCount").GetInt32() >= 2);
        Assert.True(root.GetProperty("totalCreatedCount").GetInt32() >= 2);

        var files = root.GetProperty("files").EnumerateArray().ToList();
        Assert.Equal(2, files.Count);
        Assert.Contains(files, x => x.GetProperty("cariAccountName").GetString() == firstBuyerName);
        Assert.Contains(files, x => x.GetProperty("cariAccountName").GetString() == secondBuyerName);
    }

    private static byte[] BuildDebtExcelBytes()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "Tarih";
        ws.Cell(1, 2).Value = "Malzeme Aciklama";
        ws.Cell(1, 3).Value = "Adet";
        ws.Cell(1, 4).Value = "Satis Fiyati";
        ws.Cell(1, 5).Value = "Toplam Tutar";
        ws.Cell(1, 6).Value = "Odeme";
        ws.Cell(1, 7).Value = "Kalan Bakiye";

        ws.Cell(2, 1).Value = DateTime.UtcNow.Date;
        ws.Cell(2, 2).Value = "Bulgur";
        ws.Cell(2, 3).Value = 2;
        ws.Cell(2, 4).Value = 15;
        ws.Cell(2, 5).Value = 30;
        ws.Cell(2, 6).Value = 10;
        ws.Cell(2, 7).Value = 20;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
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
