using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class SupplierQuoteRequestItem : TenantOwnedEntity
{
    public Guid SupplierQuoteRequestId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? TargetUnitPrice { get; set; }
    public string? Notes { get; set; }
}
