namespace ERP.Application.Features.Reports.Queries.GetPurchaseSummary;

public sealed record PurchaseReportItemDto(
    DateOnly Date,
    int OrderCount,
    decimal TotalAmount,
    string TopSupplier);
