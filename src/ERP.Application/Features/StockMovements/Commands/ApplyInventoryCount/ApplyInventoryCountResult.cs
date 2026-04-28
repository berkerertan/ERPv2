namespace ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;

public sealed record ApplyInventoryCountResult(
    string ReferenceNo,
    int SubmittedItems,
    int AppliedItems,
    int SkippedItems,
    decimal TotalIncreaseQuantity,
    decimal TotalDecreaseQuantity);
