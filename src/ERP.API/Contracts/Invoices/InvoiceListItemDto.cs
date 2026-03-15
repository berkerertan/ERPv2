using ERP.Domain.Enums;

namespace ERP.API.Contracts.Invoices;

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    InvoiceType InvoiceType,
    InvoiceCategory InvoiceCategory,
    Guid? CustomerCariAccountId,
    string? CustomerName,
    Guid? SupplierCariAccountId,
    string? SupplierName,
    string TaxNumber,
    decimal TotalAmount,
    decimal TaxTotal,
    InvoiceStatus Status,
    string StatusText,
    DateTime IssueDate);
