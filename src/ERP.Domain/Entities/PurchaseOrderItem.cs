using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class PurchaseOrderItem : TenantOwnedEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

