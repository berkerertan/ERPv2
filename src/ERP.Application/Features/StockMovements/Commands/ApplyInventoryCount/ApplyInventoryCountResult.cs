namespace ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;

public sealed record ApplyInventoryCountResult(
    Guid SessionId,
    string ReferenceNo,
    int SubmittedItems,
    int AppliedItems,
    int SkippedItems,
    decimal TotalIncreaseQuantity,
    decimal TotalDecreaseQuantity);
