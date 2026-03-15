using ERP.Domain.Enums;

namespace ERP.API.Contracts.Invoices;

public sealed class CreateInvoiceRequest
{
    public string? InvoiceNumber { get; init; }
    public InvoiceType InvoiceType { get; init; }
    public InvoiceCategory InvoiceCategory { get; init; }
    public Guid CariAccountId { get; init; }
    public string? TaxNumber { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime? DueDate { get; init; }
    public string Currency { get; init; } = "TRY";
    public string? Notes { get; init; }
    public List<CreateInvoiceItemRequest> Items { get; init; } = [];
}

public sealed class CreateInvoiceItemRequest
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
    public decimal DiscountRate { get; init; }
}
