using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class QuoteItem : TenantOwnedEntity
{
    public Guid QuoteId { get; set; }
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Adet";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int SortOrder { get; set; }
}
