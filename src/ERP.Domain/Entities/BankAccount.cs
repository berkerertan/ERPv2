using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class BankAccount : TenantOwnedEntity
{
    public string BankName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal Balance { get; set; }
}

