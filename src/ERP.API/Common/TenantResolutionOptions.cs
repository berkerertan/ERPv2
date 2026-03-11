namespace ERP.API.Common;

public sealed class TenantResolutionOptions
{
    public const string SectionName = "TenantResolution";

    public string TenantCodeHeaderName { get; init; } = "X-Tenant-Code";
    public string TenantIdHeaderName { get; init; } = "X-Tenant-Id";
    public string DefaultTenantCode { get; init; } = "dev-retail";
    public bool EnableDevelopmentFallback { get; init; } = false;
}

