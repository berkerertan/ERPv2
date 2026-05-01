using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class StockMovement : TenantOwnedEntity
{
    public Guid? InventoryCountSessionId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public StockMovementReason Reason { get; set; } = StockMovementReason.ManualAdjustment;
    public string? ReasonNote { get; set; }
    public string? ProofImageUrl { get; set; }
    public string? ProofImagePublicId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime MovementDateUtc { get; set; } = DateTime.UtcNow;
    public string? ReferenceNo { get; set; }
}

