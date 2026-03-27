using ERP.Domain.Enums;

namespace ERP.API.Contracts.Waybills;

public record WaybillListDto(
    Guid Id,
    string WaybillNo,
    WaybillType Type,
    Guid CariAccountId,
    string CariName,
    Guid WarehouseId,
    string WarehouseName,
    WaybillStatus Status,
    DateTime? ShipDateUtc,
    int ItemCount,
    DateTime CreatedAtUtc);

public record WaybillItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice);

public record WaybillDto(
    Guid Id,
    string WaybillNo,
    WaybillType Type,
    Guid CariAccountId,
    string CariName,
    Guid WarehouseId,
    string WarehouseName,
    WaybillStatus Status,
    DateTime? ShipDateUtc,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<WaybillItemDto> Items,
    DateTime CreatedAtUtc);

public record CreateWaybillItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public record CreateWaybillRequest(
    WaybillType Type,
    Guid CariAccountId,
    Guid WarehouseId,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<CreateWaybillItemRequest> Items);
