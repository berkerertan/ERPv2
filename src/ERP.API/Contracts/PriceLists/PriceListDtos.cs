namespace ERP.API.Contracts.PriceLists;

public record PriceListListDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DiscountRate,
    int ItemCount,
    DateTime CreatedAtUtc);

public record PriceListItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    decimal BasePrice,
    decimal CustomPrice);

public record PriceListDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DiscountRate,
    IReadOnlyList<PriceListItemDto> Items,
    DateTime CreatedAtUtc);

public record UpsertPriceListItemRequest(Guid ProductId, decimal CustomPrice);

public record UpsertPriceListRequest(
    string Name,
    string? Description,
    bool IsActive,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DiscountRate,
    IReadOnlyList<UpsertPriceListItemRequest> Items);
