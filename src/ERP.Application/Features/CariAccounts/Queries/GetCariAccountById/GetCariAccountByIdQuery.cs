using ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccountById;

public sealed record GetCariAccountByIdQuery(Guid CariAccountId) : IRequest<CariAccountDto>;
