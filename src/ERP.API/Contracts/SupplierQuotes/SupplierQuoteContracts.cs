using ERP.Domain.Enums;

namespace ERP.API.Contracts.SupplierQuotes;

public sealed record SupplierQuoteRequestListItemDto(
    Guid Id,
    string RequestNo,
    string Title,
    Guid WarehouseId,
    string WarehouseName,
    DateTime? NeededByDateUtc,
    SupplierQuoteRequestStatus Status,
    int SupplierCount,
    int ReceivedOfferCount,
    decimal EstimatedBestTotal,
    string? CreatedByUserName,
    DateTime CreatedAtUtc);

public sealed record SupplierQuoteRequestItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal? TargetUnitPrice,
    string? Notes);

public sealed record SupplierQuoteOfferItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal OfferedQuantity,
    decimal UnitPrice,
    decimal? MinimumOrderQuantity,
    decimal LineTotal);

public sealed record SupplierQuoteOfferDto(
    Guid Id,
    Guid SupplierCariAccountId,
    string SupplierName,
    SupplierQuoteOfferStatus Status,
    int LeadTimeDays,
    string? Notes,
    DateTime? RespondedAtUtc,
    decimal TotalAmount,
    bool IsSelected,
    IReadOnlyList<SupplierQuoteOfferItemDto> Items);

public sealed record SupplierQuoteRequestDetailDto(
    Guid Id,
    string RequestNo,
    string Title,
    Guid WarehouseId,
    string WarehouseName,
    DateTime? NeededByDateUtc,
    SupplierQuoteRequestStatus Status,
    string? Notes,
    string? CreatedByUserName,
    DateTime CreatedAtUtc,
    Guid? SelectedSupplierCariAccountId,
    Guid? SelectedOfferId,
    IReadOnlyList<SupplierQuoteRequestItemDto> Items,
    IReadOnlyList<SupplierQuoteOfferDto> Offers);

public sealed class CreateSupplierQuoteRequestRequest
{
    public string Title { get; init; } = string.Empty;
    public Guid WarehouseId { get; init; }
    public DateTime? NeededByDateUtc { get; init; }
    public string? Notes { get; init; }
    public List<Guid> SupplierCariAccountIds { get; init; } = [];
    public List<CreateSupplierQuoteRequestItemRequest> Items { get; init; } = [];
}

public sealed class CreateSupplierQuoteRequestItemRequest
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal? TargetUnitPrice { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpsertSupplierQuoteOfferRequest
{
    public SupplierQuoteOfferStatus Status { get; init; } = SupplierQuoteOfferStatus.Received;
    public int LeadTimeDays { get; init; }
    public string? Notes { get; init; }
    public List<UpsertSupplierQuoteOfferItemRequest> Items { get; init; } = [];
}

public sealed class UpsertSupplierQuoteOfferItemRequest
{
    public Guid ProductId { get; init; }
    public decimal OfferedQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal? MinimumOrderQuantity { get; init; }
}

public sealed class SelectSupplierQuoteOfferRequest
{
    public Guid OfferId { get; init; }
}

public sealed record ConvertSupplierQuoteResultDto(Guid PurchaseOrderId, string OrderNo);
