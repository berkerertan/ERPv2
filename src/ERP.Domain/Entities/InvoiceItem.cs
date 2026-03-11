using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class InvoiceItem : TenantOwnedEntity
{
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "EA";
    public decimal UnitPrice { get; set; }

    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

