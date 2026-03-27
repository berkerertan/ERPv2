using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class PriceListItem : TenantOwnedEntity
{
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public decimal CustomPrice { get; set; }
}
