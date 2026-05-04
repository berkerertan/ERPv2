using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class SupplierQuoteOffer : TenantOwnedEntity
{
    public Guid SupplierQuoteRequestId { get; set; }
    public Guid SupplierCariAccountId { get; set; }
    public SupplierQuoteOfferStatus Status { get; set; } = SupplierQuoteOfferStatus.Pending;
    public int LeadTimeDays { get; set; }
    public string? Notes { get; set; }
    public DateTime? RespondedAtUtc { get; set; }

    public ICollection<SupplierQuoteOfferItem> Items { get; set; } = [];
}
