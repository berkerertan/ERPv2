using ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariDebtItemById;

public sealed record GetCariDebtItemByIdQuery(Guid CariAccountId, Guid CariDebtItemId) : IRequest<CariDebtItemDto>;
