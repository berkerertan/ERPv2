namespace ERP.Application.Features.Reports.Queries.GetPurchaseSummary;

public sealed record PurchaseSummaryDto(int ApprovedOrderCount, decimal TotalPurchaseAmount, decimal TotalPurchasedQuantity);
