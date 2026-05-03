using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class InventoryCountSession : TenantOwnedEntity
{
    public string? ClientRequestId { get; set; }
    public Guid WarehouseId { get; set; }
    public InventoryCountSessionStatus Status { get; set; } = InventoryCountSessionStatus.Open;
    public string ReferenceNo { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? LocationCode { get; set; }
    public Guid? StartedByUserId { get; set; }
    public string? StartedByUserName { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int SubmittedItems { get; set; }
    public int AppliedItems { get; set; }
    public int SkippedItems { get; set; }
    public decimal TotalIncreaseQuantity { get; set; }
    public decimal TotalDecreaseQuantity { get; set; }

    public List<InventoryCountSessionItem> Items { get; set; } = [];
}
