using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class PurchaseOrder : TenantOwnedEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public Guid SupplierCariAccountId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancelledByUserName { get; set; }
    public string? CancellationReason { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}

