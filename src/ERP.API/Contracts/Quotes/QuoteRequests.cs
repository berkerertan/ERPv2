using ERP.Domain.Enums;

namespace ERP.API.Contracts.Quotes;

public sealed class UpsertQuoteRequest
{
    public string QuoteNumber { get; init; } = string.Empty;
    public Guid? CariAccountId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerEmail { get; init; }
    public DateTime QuoteDateUtc { get; init; } = DateTime.UtcNow;
    public DateTime ValidUntilUtc { get; init; }
    public decimal OverallDiscountPercent { get; init; }
    public decimal TaxPercent { get; init; } = 18;
    public string? Notes { get; init; }
    public IReadOnlyList<UpsertQuoteItemRequest> Items { get; init; } = [];
}

public sealed class UpsertQuoteItemRequest
{
    public Guid? ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Unit { get; init; } = "Adet";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
}

public sealed class UpdateQuoteStatusRequest
{
    public QuoteStatus Status { get; init; }
}

public sealed class ConvertToOrderRequest
{
    public Guid WarehouseId { get; init; }
}
