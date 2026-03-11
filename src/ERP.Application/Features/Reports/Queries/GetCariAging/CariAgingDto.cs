namespace ERP.Application.Features.Reports.Queries.GetCariAging;

public sealed record CariAgingDto(
    Guid CariAccountId,
    string Name,
    decimal Current,
    decimal Days30,
    decimal Days60,
    decimal Days90,
    decimal Over90,
    decimal Total);
