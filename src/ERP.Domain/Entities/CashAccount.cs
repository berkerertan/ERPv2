using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class CashAccount : TenantOwnedEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal Balance { get; set; }
}

