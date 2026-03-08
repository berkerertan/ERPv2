using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.TransferStock;

public sealed record TransferStockCommand(
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    string? ReferenceNo) : IRequest<TransferStockResult>;
