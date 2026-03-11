using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Company : TenantOwnedEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
}

