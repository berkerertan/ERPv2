using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class ReturnItem : TenantOwnedEntity
{
    public Guid ReturnId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
