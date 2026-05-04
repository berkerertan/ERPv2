using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class SupplierQuoteOfferItem : TenantOwnedEntity
{
    public Guid SupplierQuoteOfferId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OfferedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? MinimumOrderQuantity { get; set; }
}
