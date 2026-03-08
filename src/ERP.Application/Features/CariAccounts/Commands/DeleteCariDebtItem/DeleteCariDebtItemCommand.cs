using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.DeleteCariDebtItem;

public sealed record DeleteCariDebtItemCommand(Guid CariAccountId, Guid CariDebtItemId) : IRequest;
