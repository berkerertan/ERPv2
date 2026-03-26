using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ERP.API.IntegrationTests;

public sealed class ProductImageCloudStorageTests
{
    [Fact]
    public async Task Upload_Image_Should_Return_503_When_Cloud_Storage_Not_Configured()
    {
        await using var factory = new ErpApiWebApplicationFactory(enforceAuthorization: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await LoginAsync(client, "demo", "Test123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateProductAsync(client, $"IMG-P-{Guid.NewGuid():N}".Substring(0, 20), "Image Product");

        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("fake-image-content");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "product.png");

        var uploadResponse = await client.PostAsync($"/api/products/{productId}/image", content);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, uploadResponse.StatusCode);
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
            category = "Cloud",
            barcodeEan13 = (string?)null,
            qrCode = (string?)null,
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
