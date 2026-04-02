namespace ERP.Domain.Enums;

public enum StockMovementReason
{
    ManualAdjustment = 1,
    PurchaseApproval = 2,
    SalesApproval = 3,
    TransferOut = 4,
    TransferIn = 5,
    PosSale = 6,
    InventoryAdjustment = 7,
    WasteScrap = 8,
    ReturnIn = 9,
    ReturnOut = 10
}
