using ERP.Domain.Enums;

namespace ERP.API.Contracts.Returns;

public record ReturnListDto(
    Guid Id,
    string ReturnNo,
    ReturnType Type,
    Guid CariAccountId,
    string CariName,
    ReturnStatus Status,
    DateTime ReturnDateUtc,
    int ItemCount,
    decimal TotalAmount,
    DateTime CreatedAtUtc);

public record ReturnItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record ReturnDto(
    Guid Id,
    string ReturnNo,
    ReturnType Type,
    Guid CariAccountId,
    string CariName,
    Guid WarehouseId,
    string WarehouseName,
    ReturnStatus Status,
    DateTime ReturnDateUtc,
    string? Reason,
    IReadOnlyList<ReturnItemDto> Items,
    decimal TotalAmount,
    DateTime CreatedAtUtc);

public record CreateReturnItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public record CreateReturnRequest(
    ReturnType Type,
    Guid CariAccountId,
    Guid WarehouseId,
    string? Reason,
    IReadOnlyList<CreateReturnItemRequest> Items);
