namespace ERP.Application.Features.StockMovements.Commands.TransferStock;

public sealed record TransferStockResult(
    Guid OutMovementId,
    Guid InMovementId,
    string ReferenceNo,
    DateTime MovementDateUtc);
