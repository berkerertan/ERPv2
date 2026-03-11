namespace ERP.Application.Features.Reports.Queries.GetSalesSummary;

public sealed record SalesReportItemDto(
    DateOnly Date,
    int OrderCount,
    decimal TotalAmount,
    string TopProduct);
