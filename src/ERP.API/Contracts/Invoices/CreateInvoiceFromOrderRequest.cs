using ERP.Domain.Enums;

namespace ERP.API.Contracts.Invoices;

public sealed class CreateInvoiceFromOrderRequest
{
    public InvoiceType InvoiceType { get; init; } = InvoiceType.EFatura;
    public DateTime? IssueDate { get; init; }
    public DateTime? DueDate { get; init; }
    public string Currency { get; init; } = "TRY";
    public string? Notes { get; init; }
}
