using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class PriceList : TenantOwnedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DiscountRate { get; set; }

    public ICollection<PriceListItem> Items { get; set; } = [];
}
