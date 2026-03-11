using ERP.Domain.Enums;

namespace ERP.API.Contracts.Invoices;

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    InvoiceType InvoiceType,
    InvoiceCategory InvoiceCategory,
    InvoiceStatus Status,
    Guid CariAccountId,
    string CariAccountName,
    string TaxNumber,
    DateTime IssueDate,
    DateTime? DueDate,
    decimal Subtotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    string Currency,
    string? Uuid,
    string? Ettn,
    string? GibResponseCode,
    string? GibResponseDescription,
    string? Notes,
    DateTime CreatedAt,
    Guid? SalesOrderId,
    Guid? PurchaseOrderId);
