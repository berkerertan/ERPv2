namespace ERP.API.Contracts.Invoices;

public sealed record InvoiceItemDto(
    Guid Id,
    Guid InvoiceId,
    Guid ProductId,
    string ProductName,
    string Barcode,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal DiscountAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal LineTotal);
