using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class InventoryCountSessionItem : TenantOwnedEntity
{
    public Guid InventoryCountSessionId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? LocationCode { get; set; }
    public Guid? CountedByUserId { get; set; }
    public string? CountedByUserName { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public DateTime CountedAtUtc { get; set; } = DateTime.UtcNow;
}
