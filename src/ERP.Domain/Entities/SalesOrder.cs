using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class SalesOrder : BaseEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public Guid CustomerCariAccountId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public ICollection<SalesOrderItem> Items { get; set; } = [];
}
