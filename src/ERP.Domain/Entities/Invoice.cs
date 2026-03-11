using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class Invoice : TenantOwnedEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public InvoiceCategory InvoiceCategory { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public Guid CariAccountId { get; set; }
    public string CariAccountName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;

    public Guid? SalesOrderId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    public DateTime IssueDateUtc { get; set; }
    public DateTime? DueDateUtc { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public string Currency { get; set; } = "TRY";
    public string? Uuid { get; set; }
    public string? Ettn { get; set; }
    public string? GibResponseCode { get; set; }
    public string? GibResponseDescription { get; set; }
    public string? Notes { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = [];
}

