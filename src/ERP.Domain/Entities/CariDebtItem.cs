using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class CariDebtItem : BaseEntity
{
    public Guid CariAccountId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string MaterialDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Payment { get; set; }
    public decimal RemainingBalance { get; set; }

    public CariAccount? CariAccount { get; set; }
}
