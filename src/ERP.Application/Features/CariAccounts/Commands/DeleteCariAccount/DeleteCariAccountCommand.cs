using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.DeleteCariAccount;

public sealed record DeleteCariAccountCommand(Guid CariAccountId) : IRequest;
