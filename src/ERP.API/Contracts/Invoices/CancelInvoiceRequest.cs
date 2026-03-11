namespace ERP.API.Contracts.Invoices;

public sealed class CancelInvoiceRequest
{
    public string Reason { get; init; } = string.Empty;
}
