using ERP.Domain.Enums;

namespace ERP.API.Contracts.Invoices;

public sealed class UpdateInvoiceRequest
{
    public string? InvoiceNumber { get; init; }
    public InvoiceType InvoiceType { get; init; }
    public InvoiceCategory InvoiceCategory { get; init; }
    public string? TaxNumber { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime? DueDate { get; init; }
    public string Currency { get; init; } = "TRY";
    public string? Notes { get; init; }
    public List<CreateInvoiceItemRequest> Items { get; init; } = [];
}
