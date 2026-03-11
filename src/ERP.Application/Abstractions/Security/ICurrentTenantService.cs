namespace ERP.Application.Abstractions.Security;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    string? TenantCode { get; }
    bool HasTenant { get; }
    bool IsPlatformAdmin { get; }
}
