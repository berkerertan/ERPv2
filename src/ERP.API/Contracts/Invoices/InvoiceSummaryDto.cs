namespace ERP.API.Contracts.Invoices;

public sealed record InvoiceSummaryDto(
    int TotalCount,
    int DraftCount,
    int SentCount,
    int ApprovedCount,
    int RejectedCount,
    decimal TotalAmount);
