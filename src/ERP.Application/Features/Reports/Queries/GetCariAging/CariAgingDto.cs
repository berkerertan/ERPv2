namespace ERP.Application.Features.Reports.Queries.GetCariAging;

public sealed record CariAgingDto(
    Guid CariAccountId,
    string Code,
    string Name,
    int MaturityDays,
    decimal CurrentBalance,
    string AgingBucket);
