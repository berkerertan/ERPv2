namespace ERP.API.Contracts.Invoices;

public sealed record InvoiceDetailDto(
    InvoiceDto Invoice,
    IReadOnlyList<InvoiceItemDto> Items,
    Guid? CustomerCariAccountId,
    string? CustomerName,
    Guid? SupplierCariAccountId,
    string? SupplierName);
