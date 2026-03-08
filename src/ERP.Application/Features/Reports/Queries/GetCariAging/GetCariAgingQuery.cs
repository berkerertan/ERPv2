using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetCariAging;

public sealed record GetCariAgingQuery : IRequest<IReadOnlyList<CariAgingDto>>;
