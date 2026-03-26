using ERP.Domain.Enums;

namespace ERP.API.Contracts.Quotes;

public sealed record QuoteDto(
    Guid Id,
    string QuoteNumber,
    Guid? CariAccountId,
    string? CariCode,
    string? CariName,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    QuoteStatus Status,
    DateTime QuoteDateUtc,
    DateTime ValidUntilUtc,
    decimal OverallDiscountPercent,
    decimal TaxPercent,
    string? Notes,
    Guid? ConvertedSalesOrderId,
    IReadOnlyList<QuoteItemDto> Items,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    DateTime CreatedAtUtc);

public sealed record QuoteItemDto(
    Guid Id,
    Guid? ProductId,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal,
    int SortOrder);

public sealed record QuoteListDto(
    Guid Id,
    string QuoteNumber,
    string CustomerName,
    QuoteStatus Status,
    DateTime QuoteDateUtc,
    DateTime ValidUntilUtc,
    int ItemCount,
    decimal GrandTotal,
    DateTime CreatedAtUtc);
