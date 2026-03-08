namespace ERP.Application.Features.Pos.Commands.CreatePosQuickSale;

public sealed record PosQuickSaleResult(
    Guid SalesOrderId,
    string OrderNo,
    decimal TotalAmount,
    int ItemCount,
    DateTime SaleDateUtc);
