using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;

public sealed record GetCariDebtItemsQuery(Guid CariAccountId) : IRequest<IReadOnlyList<CariDebtItemDto>>;
