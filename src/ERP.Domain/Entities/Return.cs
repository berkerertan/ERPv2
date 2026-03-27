using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class Return : TenantOwnedEntity
{
    public string ReturnNo { get; set; } = string.Empty;
    public ReturnType Type { get; set; } = ReturnType.Sales;
    public Guid CariAccountId { get; set; }
    public Guid WarehouseId { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
    public DateTime ReturnDateUtc { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    public ICollection<ReturnItem> Items { get; set; } = [];
}
