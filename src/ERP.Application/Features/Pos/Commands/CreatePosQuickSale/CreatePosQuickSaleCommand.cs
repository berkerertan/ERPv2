using MediatR;

namespace ERP.Application.Features.Pos.Commands.CreatePosQuickSale;

public sealed record PosQuickSaleItemInput(Guid? ProductId, string? Barcode, decimal Quantity, decimal? UnitPrice);

public sealed record CreatePosQuickSaleCommand(
    Guid CustomerCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<PosQuickSaleItemInput> Items,
    string? Note) : IRequest<PosQuickSaleResult>;
