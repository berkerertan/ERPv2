using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ERP.API.IntegrationTests;

public sealed class ErpApiWebApplicationFactory(
    bool enforceAuthorization,
    IReadOnlyDictionary<string, string?>? additionalSettings = null) : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ERPv2Test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
                ["Security:EnforceAuthorization"] = enforceAuthorization ? "true" : "false",
                ["TenantResolution:EnableDevelopmentFallback"] = "true",
                ["TenantResolution:DefaultTenantCode"] = "demo-tier3",
                ["Cloudinary:Enabled"] = "false",
                ["Cloudinary:CloudName"] = "",
                ["Cloudinary:ApiKey"] = "",
                ["Cloudinary:ApiSecret"] = "",
                ["Smtp:Enabled"] = "false"
            };

            if (additionalSettings is not null)
            {
                foreach (var setting in additionalSettings)
                {
                    testSettings[setting.Key] = setting.Value;
                }
            }

            configBuilder.AddInMemoryCollection(testSettings);
        });
    }
}
