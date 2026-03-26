using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class Quote : TenantOwnedEntity
{
    public string QuoteNumber { get; set; } = string.Empty;
    public Guid? CariAccountId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public DateTime QuoteDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntilUtc { get; set; }
    public decimal OverallDiscountPercent { get; set; }
    public decimal TaxPercent { get; set; } = 18;
    public string? Notes { get; set; }
    public Guid? ConvertedSalesOrderId { get; set; }

    public ICollection<QuoteItem> Items { get; set; } = [];
}
