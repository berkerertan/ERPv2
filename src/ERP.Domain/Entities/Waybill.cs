using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class Waybill : TenantOwnedEntity
{
    public string WaybillNo { get; set; } = string.Empty;
    public WaybillType Type { get; set; } = WaybillType.Outgoing;
    public Guid CariAccountId { get; set; }
    public Guid WarehouseId { get; set; }
    public WaybillStatus Status { get; set; } = WaybillStatus.Draft;
    public DateTime? ShipDateUtc { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }

    public ICollection<WaybillItem> Items { get; set; } = [];
}
