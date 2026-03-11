using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class FinanceMovement : TenantOwnedEntity
{
    public Guid CariAccountId { get; set; }
    public FinanceMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime MovementDateUtc { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string? ReferenceNo { get; set; }
}

