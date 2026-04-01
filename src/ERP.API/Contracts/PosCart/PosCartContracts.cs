namespace ERP.API.Contracts.PosCart;

public sealed record PosCartItemContract(
    Guid? ProductId,
    string Name,
    string Barcode,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record SavePosCartRequest(
    Guid? Id,
    string? Label,
    Guid? BuyerId,
    string? BuyerName,
    string? PaymentMethod,
    Guid WarehouseId,
    IReadOnlyList<PosCartItemContract> Items);

public sealed record SavePosCartResponse(
    Guid Id,
    string ShareToken,
    string Label,
    DateTime UpdatedAt);

public sealed record PosCartSummaryResponse(
    Guid Id,
    string ShareToken,
    string Label,
    string? BuyerName,
    string PaymentMethod,
    int ItemCount,
    decimal GrandTotal,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PosCartDetailResponse(
    Guid Id,
    string ShareToken,
    string Label,
    Guid? BuyerId,
    string? BuyerName,
    string PaymentMethod,
    Guid WarehouseId,
    IReadOnlyList<PosCartItemContract> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt);
