using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class CheckNotesTests
{
    [Fact]
    public async Task CheckNote_Crud_Status_And_Settle_Should_Work()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var cariId = await CreateCariAccountAsync(client, $"BCH{Guid.NewGuid():N}".Substring(0, 12), "Test Buyer BCH", cariType: 1);
        var cashAccountId = await CreateCashAccountAsync(client, $"CA{Guid.NewGuid():N}".Substring(0, 12), "Check Cash");

        var createResponse = await client.PostAsJsonAsync("/api/accounting/check-notes", new
        {
            code = $"CH{Guid.NewGuid():N}".Substring(0, 14),
            type = 1,
            direction = 1,
            cariAccountId = cariId,
            amount = 12500.50m,
            currency = "TRY",
            issueDateUtc = DateTime.UtcNow.Date,
            dueDateUtc = DateTime.UtcNow.Date.AddDays(20),
            bankName = "Test Bank",
            branchName = "Main",
            accountNo = "12345",
            serialNo = "SN-001",
            description = "Vadeli satis cek kaydi"
        });
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Create check note failed: {(int)createResponse.StatusCode} - {error}");
        }
        var checkNoteId = await DeserializeGuidAsync(createResponse);

        var listResponse = await client.GetAsync("/api/accounting/check-notes?direction=1");
        listResponse.EnsureSuccessStatusCode();
        using (var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()))
        {
            var hasCreated = listDoc.RootElement.EnumerateArray()
                .Any(x => x.GetProperty("id").GetGuid() == checkNoteId);
            Assert.True(hasCreated);
        }

        var statusResponse = await client.PostAsJsonAsync($"/api/accounting/check-notes/{checkNoteId}/status", new
        {
            status = 2,
            note = "Ciro edildi"
        });
        Assert.Equal(HttpStatusCode.NoContent, statusResponse.StatusCode);

        var settleResponse = await client.PostAsJsonAsync($"/api/accounting/check-notes/{checkNoteId}/settle", new
        {
            channel = 1,
            treasuryAccountId = cashAccountId,
            transactionDateUtc = DateTime.UtcNow,
            description = "Tahsil edildi",
            referenceNo = "CHK-COLLECT-001"
        });
        settleResponse.EnsureSuccessStatusCode();

        using (var settleDoc = JsonDocument.Parse(await settleResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(checkNoteId, settleDoc.RootElement.GetProperty("checkNoteId").GetGuid());
            Assert.Equal(4, settleDoc.RootElement.GetProperty("status").GetInt32());
            Assert.True(settleDoc.RootElement.GetProperty("financeMovementId").GetGuid() != Guid.Empty);
        }

        var detailResponse = await client.GetAsync($"/api/accounting/check-notes/{checkNoteId}");
        detailResponse.EnsureSuccessStatusCode();
        using var detailDoc = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(4, detailDoc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Receivable_CheckNote_Should_Reject_Supplier_Cari_On_Settle()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var supplierCariId = await CreateCariAccountAsync(client, $"SUP{Guid.NewGuid():N}".Substring(0, 12), "Test Supplier", cariType: 2);
        var cashAccountId = await CreateCashAccountAsync(client, $"CB{Guid.NewGuid():N}".Substring(0, 12), "Check Cash 2");

        var createResponse = await client.PostAsJsonAsync("/api/accounting/check-notes", new
        {
            code = $"SN{Guid.NewGuid():N}".Substring(0, 14),
            type = 2,
            direction = 1,
            cariAccountId = supplierCariId,
            amount = 2500m,
            currency = "TRY",
            issueDateUtc = DateTime.UtcNow.Date,
            dueDateUtc = DateTime.UtcNow.Date.AddDays(10)
        });
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Create check note failed: {(int)createResponse.StatusCode} - {error}");
        }
        var checkNoteId = await DeserializeGuidAsync(createResponse);

        var settleResponse = await client.PostAsJsonAsync($"/api/accounting/check-notes/{checkNoteId}/settle", new
        {
            channel = 1,
            treasuryAccountId = cashAccountId
        });

        Assert.Equal(HttpStatusCode.Conflict, settleResponse.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token missing in login response.");
    }

    private static async Task<Guid> CreateCariAccountAsync(HttpClient client, string code, string name, int cariType)
    {
        var response = await client.PostAsJsonAsync("/api/cari-accounts", new
        {
            code,
            name,
            type = cariType,
            riskLimit = 100000m,
            maturityDays = 30,
            phone = "5550000000"
        });
        response.EnsureSuccessStatusCode();
        return await DeserializeGuidAsync(response);
    }

    private static async Task<Guid> CreateCashAccountAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/accounting/cash-accounts", new
        {
            code,
            name,
            currency = "TRY"
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
