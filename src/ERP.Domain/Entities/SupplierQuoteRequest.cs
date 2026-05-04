using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class SupplierQuoteRequest : TenantOwnedEntity
{
    public string RequestNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public DateTime? NeededByDateUtc { get; set; }
    public SupplierQuoteRequestStatus Status { get; set; } = SupplierQuoteRequestStatus.Open;
    public string? Notes { get; set; }
    public string? CreatedByUserName { get; set; }
    public Guid? SelectedSupplierCariAccountId { get; set; }
    public Guid? SelectedOfferId { get; set; }

    public ICollection<SupplierQuoteRequestItem> Items { get; set; } = [];
    public ICollection<SupplierQuoteOffer> Offers { get; set; } = [];
}
