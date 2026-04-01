using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class PosCart : TenantOwnedEntity
{
    public string Label { get; set; } = string.Empty;
    public string ShareToken { get; set; } = string.Empty;
    public Guid? BuyerId { get; set; }
    public string? BuyerName { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public Guid WarehouseId { get; set; }
    public string ItemsJson { get; set; } = "[]";
}
