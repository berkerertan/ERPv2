namespace ERP.Application.Features.Reports.Queries.GetSalesSummary;

public sealed record SalesSummaryDto(int ApprovedOrderCount, decimal TotalSalesAmount, decimal TotalSoldQuantity);
